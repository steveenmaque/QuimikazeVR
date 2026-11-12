using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LabQuimicaVR
{
    /// <summary>Aula local sin cuentas. Toda la configuración se puede completar con mirada y botón del visor.</summary>
    public sealed class LanClassroom : MonoBehaviour
    {
        static LanClassroom instance;
        LanSession session;
        Transform modal;
        GazeButton launcher;
        string address = "192.168.", alias, notice = "";
        bool dirty;
        int panelClosedFrame = -1;
        TouchScreenKeyboard keyboard;
        float nextPose;
        string placedContext;
        readonly Dictionary<string, Transform> avatars = new Dictionary<string, Transform>();
        bool FollowsSession => session != null && session.Connected && (!session.IsHost || session.State.phase == "quiz");
        public static bool HandlesBackNavigation => instance != null && (instance.modal != null || instance.panelClosedFrame == Time.frameCount || instance.FollowsSession);
        public static Transform ActivePanel => instance != null ? instance.modal : null;
        public static bool HasActiveSession => instance != null && instance.session != null && instance.session.Connected;
        public static bool CanNavigate(string scene, int element)
        {
            if (instance == null || instance.session == null || !instance.session.Connected) return true;
            var session = instance.session;
            bool allowed = session.IsHost
                ? session.State.phase != "quiz" || (scene == LabState.ElementScene && element == session.State.element)
                : scene == session.State.worldScene && (scene != LabState.ElementScene || element == session.State.worldElement);
            if (!allowed && VRRig.Instance != null)
                VRRig.Instance.ShowToast(session.IsHost ? "Completa la ronda o cierra el aula para cambiar de elemento." : "El anfitrión elige el laboratorio. Usa Aula LAN para salir.", 3f);
            return allowed;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatic() { instance = null; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (instance != null) return;
            new GameObject("AulaLAN").AddComponent<LanClassroom>();
        }

        void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            DontDestroyOnLoad(gameObject);
            alias = "Estudiante-" + UnityEngine.Random.Range(1000, 9999);
            SceneManager.sceneLoaded += OnSceneLoaded;
            BuildLauncher();
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            modal = null;
            keyboard = null;
            placedContext = null;
            ClearAvatars();
            BuildLauncher();
        }

        void BuildLauncher()
        {
            string scene = SceneManager.GetActiveScene().name;
            if (scene != LabState.TableScene && scene != LabState.ElementScene) return;
            var pos = scene == LabState.TableScene ? new Vector3(2.8f, 1.65f, 4.8f) : new Vector3(2.8f, 1.65f, -0.6f);
            var root = new GameObject("AccesoAulaLAN").transform;
            root.position = pos;
            var spawn = scene == LabState.TableScene ? new Vector3(0, 1.65f, 1.5f) : new Vector3(0, 1.65f, 0);
            root.rotation = Quaternion.LookRotation(pos - spawn);
            launcher = LabFactory.Button(root, "Aula LAN · 2–8 jugadores", Vector3.zero, new Vector2(1.65f, 0.42f), 0.095f, Palette.Violet);
            launcher.OnClick += Open;
            LabFactory.Text(root, "Mismo Wi-Fi / punto de acceso\nSin cuentas ni Internet", new Vector3(0, -0.43f, 0), 0.085f, Palette.Ink,
                TextAlignmentOptions.Center, 1.8f, 0.5f);
        }

        void Update()
        {
            session?.Pump(Time.unscaledTime);
            SynchronizeWorld();
            UpdateAvatars();
            if (modal != null && LabInput.BackDown)
            {
                ClosePanel();
                return;
            }
            if (modal == null && FollowsSession && LabInput.BackDown)
            {
                Open();
                return;
            }
            if (keyboard != null && keyboard.status == TouchScreenKeyboard.Status.Done)
            {
                alias = LanSession.CleanAlias(keyboard.text);
                keyboard = null;
                dirty = true;
            }
            if (dirty && modal != null) { dirty = false; Draw(); }
        }

        void Open()
        {
            if (modal != null) { ClosePanel(); return; }
            var rig = VRRig.Instance;
            if (rig == null || rig.head == null) return;
            Vector3 forward = rig.head.forward;
            forward.y = 0;
            if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
            forward.Normalize();
            modal = new GameObject("PanelAulaLAN").transform;
            modal.position = rig.head.position + forward * 2.8f;
            modal.rotation = Quaternion.LookRotation(forward);
            Draw();
        }

        void Draw()
        {
            if (modal == null) return;
            LabFactory.ClearChildren(modal);
            var screen = LabFactory.Screen("Aula", modal, Vector3.zero, Quaternion.identity, new Vector2(3.8f, 3.8f), "AULA LAN · RETO COOPERATIVO", Palette.Violet);
            Button(screen, "Cerrar panel", 1.16f, 1.18f, 1.1f, ClosePanel);
            if (session == null) DrawConnection(screen);
            else DrawSession(screen);
        }

        void DrawConnection(Transform screen)
        {
            Text(screen, "Mismo Wi-Fi o punto de acceso. El anfitrión comparte su IPv4.\nExplorarán juntos: el anfitrión elige el elemento y todos lo siguen.", 0.84f, 0.082f, 0.47f);
            Text(screen, "Alias: " + alias, 1.18f, 0.095f, 0.24f, -0.51f, 2.45f);
            Button(screen, "Cambiar alias", -1.05f, 0.44f, 1.35f, ChangeAlias);
            Button(screen, "Crear aula", 0.44f, 0.44f, 1.35f, Host);
            Text(screen, "IP del anfitrión: " + address, 0.09f, 0.106f, 0.29f);
            string[] keys = { "1", "2", "3", "4", "5", "6", "7", "8", "9", ".", "0", "Borrar" };
            for (int i = 0; i < keys.Length; i++)
            {
                string key = keys[i];
                Button(screen, key, -1.2f + (i % 6) * 0.48f, -0.31f - (i / 6) * 0.4f, 0.43f,
                    () => { if (key == "Borrar") address = address.Length > 0 ? address.Substring(0, address.Length - 1) : ""; else if (address.Length < 15) address += key; dirty = true; }, 0.078f);
            }
            Button(screen, "Unirme a la IPv4", 0, -1.12f, 2.45f, Join);
            Text(screen, notice.Length == 0 ? "Puerto TCP 47771 · Máximo 8. Si falla, revisa el aislamiento Wi-Fi y el firewall." : notice, -1.48f, 0.072f, 0.4f, 0, 3.5f);
        }

        void DrawSession(Transform screen)
        {
            var state = session.State;
            Text(screen, session.IsHost ? "Anfitrión · IPv4: " + LanTransport.LocalAddresses() : "Alias: " + alias + " · Sigues al anfitrión", 0.91f, 0.082f, 0.48f);
            if (!session.Connected)
            {
                Text(screen, session.Status, 0.2f, 0.11f, 0.85f);
                Button(screen, "Volver a conectar", 0, -0.7f, 2.2f, Disconnect);
                return;
            }
            string roster = string.Join("\n", state.members.Select(m => (m.id == session.SelfId ? "Tú: " : "") + m.alias + " · " +
                (state.phase == "lobby" ? (m.ready ? "Listo" : "Esperando") : m.points + " pts" + (m.answered ? " · respondió" : ""))));
            Text(screen, roster, 0.20f, 0.085f, 1.1f, -0.95f, 1.65f);
            if (state.phase == "lobby")
            {
                Text(screen, "Objetivo del aula\nIdentificar protones, categoría y periodo de un elemento.\n3 preguntas · una respuesta por persona.\nLa explicación aparece al responder todos.", 0.18f, 0.082f, 1.05f, 0.91f, 1.65f);
                if (session.IsHost)
                {
                    bool canStart = state.members.Length >= 2 && state.members.All(m => m.ready);
                    var start = Button(screen, "Iniciar: " + ElementDatabase.Get(LabState.SelectedZ).Name, 0, -0.65f, 2.9f, () => session.StartRound(LabState.SelectedZ));
                    start.interactable = canStart;
                    Text(screen, canStart ? "El anfitrión elige el elemento explorado actualmente." : "Se necesitan al menos 2 jugadores y todos Listos.", -1f, 0.078f, 0.4f);
                }
                else
                {
                    bool ready = session.Self != null && session.Self.ready;
                    Button(screen, ready ? "Listo ✓ · cancelar" : "Estoy listo", 0, -0.65f, 2.9f, () => session.SetReady(!ready));
                    Text(screen, "El anfitrión inicia cuando todos estén listos.", -1f, 0.078f, 0.4f);
                }
            }
            else if (state.phase == "quiz")
            {
                Text(screen, (state.step + 1) + "/3 · " + ElementDatabase.Get(state.element).Name + "\n" + state.question, 0.25f, 0.1f, 1.0f, 0.89f, 1.7f);
                for (int i = 0; i < state.options.Length; i++)
                {
                    int option = i;
                    var b = Button(screen, ((char)('A' + i)) + ") " + state.options[i], -0.97f + i * 0.97f, -0.57f, 0.91f, () => session.Answer(option), 0.085f);
                    b.interactable = !state.revealed && session.Self != null && !session.Self.answered;
                }
                Text(screen, state.revealed ? state.explanation : "Investiga en las viñetas antes de responder. Al terminar, espera al grupo.", -0.98f, 0.078f, 0.45f);
                if (session.IsHost && state.revealed) Button(screen, state.step == 2 ? "Ver resultado" : "Siguiente pregunta", 0, -1.36f, 2.4f, session.Next);
                else if (session.IsHost) Button(screen, "Revelar y cerrar pregunta", 0, -1.36f, 2.8f, session.RevealQuestion, 0.08f);
            }
            else
            {
                Text(screen, "Ronda completada\nEquipo: " + state.members.Sum(m => m.correct) + " / " + state.members.Length * 3 + " aciertos.\nTu resultado: " + (session.Self?.correct ?? 0) + "/3.\nRepasa los paneles y prueba otro elemento.", 0.15f, 0.095f, 1.1f, 0.94f, 1.7f);
                if (session.IsHost) Button(screen, "Preparar otra ronda", 0, -0.75f, 2.7f, session.ReturnToLobby);
                else Text(screen, "Espera al anfitrión para la próxima ronda.", -0.82f, 0.085f, 0.45f);
            }
            Button(screen, session.IsHost ? "Cerrar aula para todos" : "Salir del aula", 0, -1.64f, 2.8f, Disconnect, 0.08f);
        }

        void Host()
        {
            try
            {
                session = CreateSession();
                session.Host(alias);
                notice = "";
            }
            catch (Exception) { session?.Dispose(); session = null; notice = "No se pudo crear el aula: puerto 47771 ocupado o red no disponible."; }
            dirty = true;
        }

        void Join()
        {
            try { session = CreateSession(); session.Join(alias, address); notice = ""; }
            catch (Exception ex) { session?.Dispose(); session = null; notice = ex.Message; }
            dirty = true;
        }

        LanSession CreateSession()
        {
            var result = new LanSession();
            result.Changed += () => dirty = true;
            result.RoundCompleted += (z, correct) => LearningProgress.RecordChallenge(z, correct, 3 - correct);
            return result;
        }

        void ChangeAlias()
        {
            if (TouchScreenKeyboard.isSupported)
                keyboard = TouchScreenKeyboard.Open(alias, TouchScreenKeyboardType.Default, false, false, false, false, "Alias del aula", 18);
            else alias = "Estudiante-" + UnityEngine.Random.Range(1000, 9999);
            dirty = true;
        }

        void Disconnect()
        {
            session?.Dispose();
            session = null;
            placedContext = null;
            ClearAvatars();
            notice = "Sesión cerrada. Puedes crear o unirte a otra aula.";
            dirty = true;
        }

        void ClosePanel()
        {
            if (modal != null) Destroy(modal.gameObject);
            modal = null;
            panelClosedFrame = Time.frameCount;
        }

        void SynchronizeWorld()
        {
            if (session == null || !session.Connected || ScreenFader.Busy) return;
            string scene = SceneManager.GetActiveScene().name;
            if (scene != LabState.TableScene && scene != LabState.ElementScene) return;
            var state = session.State;
            if (state.phase == "quiz" && (scene != LabState.ElementScene || LabState.SelectedZ != state.element))
            {
                LabState.OpenElement(state.element);
                return;
            }
            if (session.IsHost)
            {
                session.FollowElement(scene, LabState.SelectedZ);
            }
            else if (scene != state.worldScene || (scene == LabState.ElementScene && LabState.SelectedZ != state.worldElement))
            {
                if (state.worldScene == LabState.TableScene) LabState.BackToTable();
                else LabState.OpenElement(state.worldElement);
                return;
            }
            if (scene == LabState.TableScene && !session.IsHost) LabState.SelectedZ = state.worldElement;
            var rig = VRRig.Instance;
            string context = scene + ":" + state.worldElement + ":" + session.SelfId;
            if (!session.IsHost && rig != null && placedContext != context)
            {
                // Separar puntos de aparición evita superponer las cabezas de todos en el visor.
                int slot = Math.Max(1, Array.FindIndex(state.members, m => m.id == session.SelfId));
                var controller = rig.GetComponent<CharacterController>();
                bool wasEnabled = controller != null && controller.enabled;
                if (wasEnabled) controller.enabled = false;
                rig.transform.position = new Vector3((slot % 3 - 1) * 0.85f, 0, (scene == LabState.TableScene ? 1.5f : 0) + (slot / 3 + 1) * 0.7f);
                if (wasEnabled) controller.enabled = true;
                placedContext = context;
            }
            if (rig != null && rig.head != null && Time.unscaledTime >= nextPose)
            {
                nextPose = Time.unscaledTime + 0.2f;
                Vector3 pos = rig.transform.position;
                session.PublishPose(pos.x, pos.y, pos.z, rig.head.eulerAngles.y, scene, LabState.SelectedZ);
            }
        }

        void UpdateAvatars()
        {
            if (session == null || !session.Connected) { if (avatars.Count > 0) ClearAvatars(); return; }
            foreach (string id in avatars.Keys.ToArray())
            {
                if (session.State.members.All(m => m.id != id)) { if (avatars[id] != null) Destroy(avatars[id].gameObject); avatars.Remove(id); }
            }
            bool sameScene = session.State.worldScene == SceneManager.GetActiveScene().name &&
                (session.State.worldScene != LabState.ElementScene || session.State.worldElement == LabState.SelectedZ);
            foreach (var member in session.State.members)
            {
                if (member.id == session.SelfId) continue;
                if (!avatars.TryGetValue(member.id, out var root) || root == null)
                {
                    if (!member.hasPose || !sameScene) continue;
                    root = BuildAvatar(member);
                    avatars[member.id] = root;
                    root.position = new Vector3(member.x, member.y, member.z);
                }
                bool visible = member.hasPose && sameScene;
                root.gameObject.SetActive(visible);
                if (!visible) continue;
                float blend = 1 - Mathf.Exp(-10f * Time.unscaledDeltaTime);
                root.position = Vector3.Lerp(root.position, new Vector3(member.x, member.y, member.z), blend);
                root.rotation = Quaternion.Slerp(root.rotation, Quaternion.Euler(0, member.yaw, 0), blend);
            }
        }

        Transform BuildAvatar(LanMember member)
        {
            var root = LabFactory.Node("Compañero_" + member.id, transform).transform;
            Color color = member.id == "host" ? Palette.Accent2 : Palette.Violet;
            var mat = LabFactory.Tinted("Card", color);
            LabFactory.Shape("Cuerpo", PrimitiveType.Capsule, root, new Vector3(0, 0.75f, 0), new Vector3(0.38f, 0.52f, 0.38f), mat);
            LabFactory.Shape("Cabeza", PrimitiveType.Sphere, root, new Vector3(0, 1.43f, 0), Vector3.one * 0.31f, mat);
            LabFactory.Shape("Mirada", PrimitiveType.Sphere, root, new Vector3(0, 1.45f, 0.15f), new Vector3(0.16f, 0.06f, 0.05f), LabFactory.Tinted("Card", Palette.Ink));
            var label = LabFactory.Text(root, member.alias + (member.id == "host" ? " · anfitrión" : ""), new Vector3(0, 1.83f, 0), 0.085f,
                Palette.Ink, TextAlignmentOptions.Center, 1.65f, 0.3f);
            label.richText = false;
            label.gameObject.AddComponent<FaceCamera>();
            return root;
        }

        void ClearAvatars()
        {
            foreach (var root in avatars.Values) if (root != null) Destroy(root.gameObject);
            avatars.Clear();
        }

        void OnApplicationPause(bool paused)
        {
            if (!paused || session == null) return;
            Disconnect();
            notice = "El aula se cerró al suspender la app. Vuelve a conectar al regresar.";
        }
        void OnApplicationQuit() { session?.Dispose(); }
        void OnDestroy()
        {
            if (instance != this) return;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            session?.Dispose();
            instance = null;
        }

        static GazeButton Button(Transform parent, string label, float x, float y, float width, Action action, float size = 0.09f)
        {
            var b = LabFactory.Button(parent, label, new Vector3(x, y, -0.025f), new Vector2(width, 0.31f), size, Palette.Violet);
            b.OnClick += action;
            return b;
        }

        static void Text(Transform parent, string value, float y, float size, float height, float x = 0, float width = 3.45f)
        {
            var t = LabFactory.Text(parent, value, new Vector3(x, y, -0.025f), size, Color.white, TextAlignmentOptions.Center, width, height);
            t.enableAutoSizing = true;
            t.fontSizeMin = size * 7f;
            t.fontSizeMax = size * 10f;
            t.overflowMode = TextOverflowModes.Ellipsis;
            t.richText = false;
        }
    }
}

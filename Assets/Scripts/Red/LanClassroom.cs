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
        Transform quizWall, individualAnchor;
        GameObject hiddenIndividualPanel;
        int quizWallRevision = -1;
        string address = "192.168.", alias, notice = "";
        bool dirty;
        int panelClosedFrame = -1;
        int aliasStyle;
        float nextPose;
        string placedContext;
        bool menuPaused;
        int appliedSkin = -1;
        readonly Dictionary<string, Transform> avatars = new Dictionary<string, Transform>();
        readonly Dictionary<string, int> renderedSkins = new Dictionary<string, int>();
        public static bool HandlesBackNavigation => instance != null && (instance.modal != null || instance.panelClosedFrame == Time.frameCount);
        public static Transform ActivePanel => instance != null ? instance.modal : null;
        public static Transform ActiveQuizWall => instance != null ? instance.quizWall : null;
        public static bool HasActiveSession => instance != null && instance.session != null;
        public static bool CanResumeSession => HasActiveSession && instance.session.Connected && instance.session.State.explorationStarted;
        public static bool CanNavigate(string scene, int element)
        {
            if (instance == null || instance.session == null || !instance.session.Connected) return true;
            if (scene == "MenuPrincipal") return true;
            var session = instance.session;
            bool allowed = session.IsHost
                ? session.State.phase != "quiz" || (scene == LabState.ElementScene && element == session.State.element)
                : scene == session.State.worldScene && (scene != LabState.ElementScene || element == session.State.worldElement);
            if (!allowed && VRRig.Instance != null)
                VRRig.Instance.ShowToast(session.IsHost ? "Completa la ronda o cierra el aula desde el menú para cambiar de elemento." : "El anfitrión elige el laboratorio. Abre el menú para salir del aula.", 3f);
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
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            modal = null;
            quizWall = individualAnchor = null;
            hiddenIndividualPanel = null;
            quizWallRevision = -1;
            placedContext = null;
            menuPaused = scene.name == "MenuPrincipal" && session != null && session.State.explorationStarted;
            ClearAvatars();
        }

        void Update()
        {
            session?.Pump(Time.unscaledTime);
            if (appliedSkin != AvatarSkins.SelectedIndex) ApplySelectedSkin();
            SynchronizeWorld();
            UpdateQuizWall();
            UpdateAvatars();
            if (modal != null && LabInput.BackDown)
            {
                ClosePanel();
                return;
            }
            if (dirty && modal != null) { dirty = false; Draw(); }
        }

        public static void OpenFromMenu(Transform anchor)
        {
            if (anchor == null) throw new ArgumentNullException(nameof(anchor));
            if (instance == null) Bootstrap();
            instance.ClosePanel();
            instance.modal = LabFactory.Node("PanelAulaLAN", anchor).transform;
            instance.Draw();
        }

        public static void ApplySelectedSkin()
        {
            if (instance == null) return;
            instance.appliedSkin = AvatarSkins.SelectedIndex;
            instance.session?.SetSkin(instance.appliedSkin);
            instance.dirty = true;
        }

        public static void EnterSharedLab()
        {
            if (!CanResumeSession || ScreenFader.Busy) return;
            instance.menuPaused = false;
            instance.ClosePanel();
            var state = instance.session.State;
            if (state.worldScene == LabState.ElementScene) LabState.OpenElement(state.worldElement);
            else LabState.BackToTable();
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
            Text(screen, "Mismo Wi-Fi o punto de acceso. El anfitrión comparte su IPv4.\nPersonaje: " + AvatarSkins.SelectedName + ". Se cambia en el menú Personaje.", 0.84f, 0.082f, 0.47f);
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
            string roster = string.Join("\n", state.members.Select(m => (m.id == session.SelfId ? "Tú: " : "") + m.alias + " · " + (m.skin + 1) + " · " +
                (state.phase == "lobby" ? (m.ready ? "Listo" : "Esperando") : m.points + " pts" + (m.answered ? " · respondió" : ""))));
            Text(screen, roster, 0.20f, 0.085f, 1.1f, -0.95f, 1.65f);
            if (state.phase == "lobby")
            {
                Text(screen, "Objetivo del aula\nIdentificar protones, categoría y periodo de un elemento.\n3 preguntas · una respuesta por persona.\nLa explicación aparece al responder todos.", 0.18f, 0.082f, 1.05f, 0.91f, 1.65f);
                if (session.IsHost)
                {
                    bool canStart = state.members.Length >= 2 && state.members.All(m => m.ready);
                    var start = Button(screen, state.explorationStarted ? "Iniciar reto: " + ElementDatabase.Get(LabState.SelectedZ).Name : "Entrar al laboratorio juntos", 0, -0.65f, 2.9f,
                        () =>
                        {
                            if (state.explorationStarted)
                            {
                                session.StartRound(LabState.SelectedZ);
                                if (session.State.phase == "quiz") EnterSharedLab();
                            }
                            else session.StartExploration();
                        });
                    start.interactable = canStart;
                    Text(screen, canStart ? "El anfitrión inicia la exploración y elige el elemento del reto." : "Se necesitan al menos 2 jugadores y todos Listos.", -1f, 0.078f, 0.4f);
                }
                else
                {
                    bool ready = session.Self != null && session.Self.ready;
                    Button(screen, ready ? "Listo ✓ · cancelar" : "Estoy listo", 0, -0.65f, 2.9f, () => session.SetReady(!ready));
                    Text(screen, "El anfitrión inicia cuando todos estén listos.", -1f, 0.078f, 0.4f);
                }
                if (state.explorationStarted) Button(screen, "Volver al laboratorio compartido", 0, -1.36f, 3.2f, EnterSharedLab, 0.085f);
            }
            else if (state.phase == "quiz")
            {
                Text(screen, "Reto de " + ElementDatabase.Get(state.element).Name + "\nResponde en el mural del laboratorio.\nPuedes consultar las demás viñetas antes de elegir.", 0.15f, 0.095f, 1.1f, 0.94f, 1.7f);
                Button(screen, "Continuar reto en laboratorio", 0, -0.75f, 3.1f, EnterSharedLab, 0.085f);
            }
            else
            {
                Text(screen, "Ronda completada\nEquipo: " + state.members.Sum(m => m.correct) + " / " + state.members.Length * 3 + " aciertos.\nTu resultado: " + (session.Self?.correct ?? 0) + "/3.\nRepasa los paneles y prueba otro elemento.", 0.15f, 0.095f, 1.1f, 0.94f, 1.7f);
                if (session.IsHost) Button(screen, "Preparar otra ronda", 0, -0.75f, 2.7f, session.ReturnToLobby);
                else Text(screen, "Espera al anfitrión para la próxima ronda.", -0.82f, 0.085f, 0.45f);
            }
            Button(screen, session.IsHost ? "Cerrar aula para todos" : "Salir del aula", 0, -1.72f, 2.8f, Disconnect, 0.08f);
        }

        void Host()
        {
            try
            {
                session = CreateSession();
                session.Host(alias, LanTransport.DefaultPort, AvatarSkins.SelectedIndex);
                menuPaused = false;
                notice = "";
            }
            catch (Exception) { session?.Dispose(); session = null; notice = "No se pudo crear el aula: puerto 47771 ocupado o red no disponible."; }
            dirty = true;
        }

        void Join()
        {
            try { session = CreateSession(); session.Join(alias, address, LanTransport.DefaultPort, AvatarSkins.SelectedIndex); menuPaused = false; notice = ""; }
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
            string[] roles = { "Explorador", "Investigador", "Químico", "Inventor" };
            aliasStyle = (aliasStyle + 1) % roles.Length;
            alias = roles[aliasStyle] + "-" + UnityEngine.Random.Range(1000, 9999);
            dirty = true;
        }

        void Disconnect()
        {
            session?.Dispose();
            session = null;
            placedContext = null;
            menuPaused = false;
            ClearAvatars();
            RemoveQuizWall();
            notice = "Sesión cerrada. Puedes crear o unirte a otra aula.";
            dirty = true;
        }

        void ClosePanel()
        {
            if (modal != null) { modal.gameObject.SetActive(false); Destroy(modal.gameObject); }
            modal = null;
            panelClosedFrame = Time.frameCount;
        }

        void UpdateQuizWall()
        {
            bool show = session != null && session.Connected && !ScreenFader.Busy &&
                SceneManager.GetActiveScene().name == LabState.ElementScene && LabState.SelectedZ == session.State.element &&
                (session.State.phase == "quiz" || session.State.phase == "summary");
            if (!show) { RemoveQuizWall(); return; }

            if (individualAnchor == null)
            {
                var controller = FindAnyObjectByType<ElementDetailController>();
                if (controller != null) individualAnchor = controller.challengeAnchor;
            }
            var individual = individualAnchor != null ? individualAnchor.Find("PantallaReto") : null;
            if (individual != null)
            {
                hiddenIndividualPanel = individual.gameObject;
                hiddenIndividualPanel.SetActive(false);
            }
            if (quizWall == null)
            {
                // Raíz de escena independiente: el componente de reto individual permanece activo.
                quizWall = LabFactory.Node("AulaQuizMural", null, new Vector3(-3.52f, 2.80f, -6.34f), Quaternion.Euler(0, 180, 0)).transform;
                quizWallRevision = -1;
            }
            if (quizWallRevision != session.State.revision)
            {
                DrawQuizWall();
                quizWallRevision = session.State.revision;
            }
        }

        void DrawQuizWall()
        {
            if (quizWall == null || session == null) return;
            LabFactory.ClearChildren(quizWall);
            var state = session.State;
            var screen = LabFactory.Screen("PantallaRetoLAN", quizWall, Vector3.zero, Quaternion.identity, new Vector2(2.22f, 3.6f), "RETO DEL AULA LAN", Palette.Violet);
            string title = state.phase == "summary" ? "RONDA COMPLETADA" : (state.step + 1) + "/3 · " + ElementDatabase.Get(state.element).Name;
            Text(screen, title, 1.16f, 0.09f, 0.2f, 0, 1.96f);
            if (state.phase == "summary")
            {
                Text(screen, "Tus aciertos: " + (session.Self?.correct ?? 0) + "/3\nEquipo: " + state.members.Sum(m => m.correct) + "/" + state.members.Length * 3 + " aciertos", 0.73f, 0.105f, 0.55f, 0, 1.96f);
                Text(screen, string.Join("\n", state.members.Select(m => m.alias + " · " + m.correct + "/3 · " + m.points + " pts")), -0.1f, 0.078f, 0.95f, 0, 1.96f);
                Text(screen, "Repasa las explicaciones. El anfitrión prepara otra ronda desde el menú del aula.", -1.0f, 0.08f, 0.4f, 0, 1.90f);
                WallButton(screen, "Menú del aula", -1.54f, LabState.BackToMenu);
                return;
            }
            Text(screen, state.question, 0.8f, 0.095f, 0.5f, 0, 1.96f);
            for (int i = 0; i < state.options.Length; i++)
            {
                int option = i;
                var button = WallButton(screen, ((char)('A' + i)) + ") " + state.options[i], 0.32f - i * 0.36f, () => session.Answer(option));
                button.interactable = !state.revealed && session.Self != null && !session.Self.answered;
            }
            LabFactory.Card(screen, new Vector3(0, -0.97f, 0), new Vector2(2.02f, 0.68f), new Color(0.1f, 0.18f, 0.28f));
            string feedback = state.revealed ? state.explanation : session.Self != null && session.Self.answered
                ? "Tu respuesta quedó registrada. Espera al grupo; la explicación aparecerá al terminar."
                : "Investiga en las viñetas y elige una respuesta. Sin límite de tiempo.";
            Text(screen, feedback, -0.97f, 0.08f, 0.62f, 0, 1.90f);
            if (session.IsHost)
                WallButton(screen, state.revealed ? state.step == 2 ? "Ver resultado" : "Siguiente pregunta" : "Revelar y cerrar pregunta", -1.54f,
                    () => { if (session.State.revealed) session.Next(); else session.RevealQuestion(); });
            else Text(screen, "Respondieron " + state.members.Count(m => m.answered) + "/" + state.members.Length + ". El anfitrión avanza.", -1.54f, 0.075f, 0.3f, 0, 1.96f);
        }

        static GazeButton WallButton(Transform parent, string label, float y, Action action)
        {
            var button = LabFactory.Button(parent, label, new Vector3(0, y, -0.03f), new Vector2(1.96f, 0.3f), 0.085f, Palette.Violet);
            button.OnClick += action;
            button.Label.enableAutoSizing = true;
            button.Label.fontSizeMax = 0.85f;
            button.Label.fontSizeMin = 0.60f;
            button.Label.overflowMode = TextOverflowModes.Ellipsis;
            return button;
        }

        void RemoveQuizWall()
        {
            if (quizWall != null) { quizWall.gameObject.SetActive(false); Destroy(quizWall.gameObject); }
            quizWall = null;
            quizWallRevision = -1;
            if (hiddenIndividualPanel != null) hiddenIndividualPanel.SetActive(true);
            hiddenIndividualPanel = null;
        }

        void SynchronizeWorld()
        {
            if (session == null || !session.Connected || ScreenFader.Busy) return;
            string scene = SceneManager.GetActiveScene().name;
            var state = session.State;
            if (scene == "MenuPrincipal")
            {
                if (state.explorationStarted && !menuPaused) EnterSharedLab();
                return;
            }
            if (!state.explorationStarted || (scene != LabState.TableScene && scene != LabState.ElementScene)) return;
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
                if (session.State.members.All(m => m.id != id)) { if (avatars[id] != null) Destroy(avatars[id].gameObject); avatars.Remove(id); renderedSkins.Remove(id); }
            }
            bool sameScene = session.State.worldScene == SceneManager.GetActiveScene().name &&
                (session.State.worldScene != LabState.ElementScene || session.State.worldElement == LabState.SelectedZ);
            foreach (var member in session.State.members)
            {
                if (member.id == session.SelfId) continue;
                if (avatars.TryGetValue(member.id, out var previous) && previous != null && renderedSkins.TryGetValue(member.id, out int previousSkin) && previousSkin != member.skin)
                {
                    previous.gameObject.SetActive(false);
                    Destroy(previous.gameObject);
                    avatars.Remove(member.id);
                }
                if (!avatars.TryGetValue(member.id, out var root) || root == null)
                {
                    if (!member.hasPose || !sameScene) continue;
                    root = BuildAvatar(member);
                    avatars[member.id] = root;
                    renderedSkins[member.id] = member.skin;
                    root.position = new Vector3(member.x, member.y, member.z);
                }
                bool visible = member.hasPose && sameScene;
                root.gameObject.SetActive(visible);
                if (!visible) continue;
                // Mantener la sesión y las poses, sin tapar las preguntas con compañeros cercanos.
                bool showAvatar = modal == null && ScientificLibrary.ActivePanel == null;
                foreach (var renderer in root.GetComponentsInChildren<Renderer>()) renderer.enabled = showAvatar;
                float blend = 1 - Mathf.Exp(-10f * Time.unscaledDeltaTime);
                root.position = Vector3.Lerp(root.position, new Vector3(member.x, member.y, member.z), blend);
                root.rotation = Quaternion.Slerp(root.rotation, Quaternion.Euler(0, member.yaw, 0), blend);
            }
        }

        Transform BuildAvatar(LanMember member)
        {
            var root = AvatarSkins.BuildPreview(transform, member.skin);
            root.name = "Compañero_" + member.id;
            var nameplate = LabFactory.Node("Identificacion", root, new Vector3(0, 2.1f, 0)).transform;
            LabFactory.Card(nameplate, Vector3.zero, new Vector2(1.75f, 0.32f), Palette.Dark);
            var label = LabFactory.Text(nameplate, member.alias + (member.id == "host" ? " · anfitrión" : ""), new Vector3(0, 0, -0.02f), 0.085f,
                Color.white, TextAlignmentOptions.Center, 1.65f, 0.27f);
            label.richText = false;
            nameplate.gameObject.AddComponent<FaceCamera>();
            return root;
        }

        void ClearAvatars()
        {
            foreach (var root in avatars.Values) if (root != null) Destroy(root.gameObject);
            avatars.Clear();
            renderedSkins.Clear();
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
            RemoveQuizWall();
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

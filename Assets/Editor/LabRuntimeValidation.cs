using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace LabQuimicaVR.EditorTools
{
    /// <summary>
    /// Prueba real de Play. Batch: -executeMethod LabQuimicaVR.EditorTools.LabRuntimeValidation.RunBatch
    /// SIN -quit; el runner sale 0/1 al terminar. No simula sensores ni entrada física.
    /// Solo actúa cuando se invoca explícitamente. No publicar sus capturas como evidencia sin ejecutarlo.
    /// </summary>
    [InitializeOnLoad]
    public static class LabRuntimeValidation
    {
        const string Dir = "AutoSync/validacion";
        const string ReportPath = Dir + "/runtime.json";
        const string RecoveryPath = Dir + "/runtime-prefs-recovery.json";
        const string Key = "Quimikaze.RuntimeQA.";
        const string LearningKey = "Quimikaze.Learning.v1";
        const string InputKey = "LabVR.InvertRightStickY";

        [Serializable] public class Result
        {
            public bool executed, success, preferencesRestored, nativeCsvVerified, testSocketsClosed;
            public string startedUtc, finishedUtc, error, captureDirectory, csvArtifact;
            public int checks, assertionExecutions, lastStep, lanParticipants, lanQuestionsCompleted;
            public string scope = "Unity Editor Play: UI por callbacks, transiciones, persistencia nativa, modelos, reacción y tres sesiones TCP loopback reales";
            public string[] assertions = new string[0], runtimeErrors = new string[0], screenshots = new string[0];
            public string[] limitations = { "No prueba el mando físico ni sensores", "No verifica Android/IL2CPP ni Wi-Fi entre celulares",
                "Los botones se activan por sus callbacks: no prueba la precisión del raycast de mirada", "PNG monoculares requieren inspección visual; no validan la óptica estereoscópica" };
        }
        [Serializable] class Pref
        {
            public string key, type, text;
            public bool existed;
            public int number;
            public float real;
        }
        [Serializable] class PrefBackup { public Pref[] values; }
        [Serializable] class SceneBackup { public SceneSetup[] setup; }

        static Result result;
        static readonly List<string> assertions = new List<string>();
        static readonly HashSet<string> assertionNames = new HashSet<string>();
        static readonly List<string> runtimeErrors = new List<string>();
        static readonly List<string> screenshots = new List<string>();
        static int step, questionIndex, completedQuizzes, firstScore, closedOnFrame, assertionExecutions, lanQuestion, lanPanelFrame;
        static double stepStart;
        static bool ending;
        static List<ChallengeQuestion> quiz;
        static LanSession guestA, guestB, hostSession;
        static string nativeCsv;
        static float nextGuestPose;

        static LabRuntimeValidation()
        {
            EditorApplication.playModeStateChanged += PlayModeChanged;
            if (SessionState.GetBool(Key + "active", false)) Attach();
        }

        [MenuItem("Quimikaze/Validar flujo real en Play (QA)", priority = 83)]
        public static void RunMenu() => Begin(false);

        public static void RunBatch()
        {
            try { Begin(true); }
            catch (Exception ex)
            {
                // También cubre fallos anteriores a Attach: escena sucia, backup inválido o E/S.
                if (SessionState.GetBool(Key + "active", false)) { Finish(ex.ToString()); return; }
                result = new Result { error = ex.ToString(), startedUtc = DateTime.UtcNow.ToString("o"), finishedUtc = DateTime.UtcNow.ToString("o") };
                string cleanup = CleanupScenario();
                if (!string.IsNullOrEmpty(cleanup)) result.error += "\nLimpieza:" + cleanup;
                try { RestorePreferences(); result.preferencesRestored = true; }
                catch (Exception restore) { result.error += "\nRestauración:" + restore; }
                try { Directory.CreateDirectory(Dir); PersistResult(); }
                finally { EditorApplication.Exit(1); }
            }
        }

        static void Begin(bool batch)
        {
            if (SessionState.GetBool(Key + "active", false)) throw new InvalidOperationException("Ya existe una prueba Play en curso.");
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Sal de Play antes de iniciar QA.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Guarda tus escenas modificadas antes de ejecutar QA.");
            Directory.CreateDirectory(Dir);
            ending = false;
            // Si una ejecución anterior se interrumpió abruptamente, se recupera antes de comenzar otra.
            if (File.Exists(RecoveryPath)) RestorePreferences();
            File.WriteAllText(RecoveryPath, JsonUtility.ToJson(new PrefBackup
            {
                values = new[] { Snapshot(LearningKey, "text"), Snapshot(InputKey, "int"),
                    Snapshot("LabVR.LensMm", "float"), Snapshot("LabVR.Orientacion", "int") }
            }));
            SessionState.SetString(Key + "scenes", JsonUtility.ToJson(new SceneBackup { setup = EditorSceneManager.GetSceneManagerSetup() }));
            SessionState.SetBool(Key + "batch", batch);
            SessionState.SetBool(Key + "active", true);
            SessionState.SetBool(Key + "ending", false);
            SessionState.SetString(Key + "start", EditorApplication.timeSinceStartup.ToString("R", CultureInfo.InvariantCulture));
            SessionState.SetString(Key + "utc", DateTime.UtcNow.ToString("o"));
            SessionState.SetString(Key + "captures", Dir + "/runtime_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff"));
            assertions.Clear(); assertionNames.Clear(); runtimeErrors.Clear(); screenshots.Clear();
            assertionExecutions = 0;
            result = new Result { startedUtc = SessionState.GetString(Key + "utc", "") };
            PersistResult();
            foreach (var pref in JsonUtility.FromJson<PrefBackup>(File.ReadAllText(RecoveryPath)).values) PlayerPrefs.DeleteKey(pref.key);
            PlayerPrefs.Save();
            Attach();
            try
            {
                EditorSceneManager.OpenScene(LabSceneBuilder.TableScenePath);
                EditorApplication.EnterPlaymode();
            }
            catch (Exception ex) { Finish(ex.ToString()); }
        }

        static Pref Snapshot(string key, string type) => new Pref
        {
            key = key, type = type, existed = PlayerPrefs.HasKey(key),
            text = type == "text" ? PlayerPrefs.GetString(key, "") : "",
            number = type == "int" ? PlayerPrefs.GetInt(key, 0) : 0,
            real = type == "float" ? PlayerPrefs.GetFloat(key, 0) : 0
        };

        static void RestorePreferences()
        {
            if (!File.Exists(RecoveryPath)) return;
            var backup = JsonUtility.FromJson<PrefBackup>(File.ReadAllText(RecoveryPath));
            if (backup == null || backup.values == null) throw new IOException("Backup de preferencias QA inválido; se conserva para recuperación manual.");
            foreach (var p in backup.values)
            {
                if (!p.existed) PlayerPrefs.DeleteKey(p.key);
                else if (p.type == "text") PlayerPrefs.SetString(p.key, p.text);
                else if (p.type == "int") PlayerPrefs.SetInt(p.key, p.number);
                else if (p.type == "float") PlayerPrefs.SetFloat(p.key, p.real);
            }
            PlayerPrefs.Save();
            foreach (var p in backup.values)
            {
                bool same = PlayerPrefs.HasKey(p.key) == p.existed;
                if (same && p.existed)
                    same = p.type == "text" ? PlayerPrefs.GetString(p.key) == p.text :
                        p.type == "int" ? PlayerPrefs.GetInt(p.key) == p.number : PlayerPrefs.GetFloat(p.key) == p.real;
                if (!same) throw new IOException("No se pudieron restaurar preferencias de QA; se conserva el backup.");
            }
            // Solo este archivo propio, nunca capturas ni documentos del usuario.
            File.Delete(RecoveryPath);
        }

        static void Attach()
        {
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            Application.logMessageReceived -= Log;
            Application.logMessageReceived += Log;
        }

        static void PlayModeChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(Key + "active", false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                ending = false;
                result = new Result { executed = true, startedUtc = SessionState.GetString(Key + "utc", ""),
                    captureDirectory = SessionState.GetString(Key + "captures", Dir + "/runtime") };
                Directory.CreateDirectory(result.captureDirectory);
                assertions.Clear(); assertionNames.Clear(); screenshots.Clear();
                step = questionIndex = completedQuizzes = firstScore = assertionExecutions = lanQuestion = 0;
                stepStart = EditorApplication.timeSinceStartup;
                Attach();
            }
            else if (state == PlayModeStateChange.ExitingPlayMode && !SessionState.GetBool(Key + "ending", false))
                Finish("La prueba se detuvo antes de completar los pasos.", false);
            else if (state == PlayModeStateChange.EnteredEditMode) FinalizeInEditMode();
        }

        static void Log(string message, string stack, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            runtimeErrors.Add(type + ": " + message + "\n" + stack);
        }

        static void Assert(bool condition, string description)
        {
            assertionExecutions++;
            if (!condition) throw new InvalidOperationException("Play QA: " + description);
            if (assertionNames.Add(description)) assertions.Add(description);
        }
        static bool After(double seconds) => EditorApplication.timeSinceStartup - stepStart >= seconds;
        static void Next(int target = -1) { step = target < 0 ? step + 1 : target; stepStart = EditorApplication.timeSinceStartup; }
        static T Find<T>() where T : Object => Object.FindAnyObjectByType<T>();
        static T Field<T>(object instance, string field) => (T)instance.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(instance);
        static GazeButton Button(Transform root, Func<string, bool> match)
        {
            var button = root.GetComponentsInChildren<GazeButton>(false).FirstOrDefault(b => b.gameObject.activeInHierarchy && b.Interactable && b.Label != null && match(b.Label.text));
            Assert(button != null, "Botón visible e interactuable: " + root.name);
            return button;
        }
        static void Click(Transform root, Func<string, bool> match) => Button(root, match).Click();
        static void RequireScene(string scene)
        {
            if (!After(15)) return;
            throw new TimeoutException("No terminó la transición a " + scene + " dentro de 15 s.");
        }

        static bool WaitFor(bool ready, string detail, double seconds = 15)
        {
            if (ready) return true;
            if (After(seconds)) throw new TimeoutException("Play QA: " + detail);
            return false;
        }

        static bool HasButton(Transform root, Func<string, bool> match) => root != null &&
            root.GetComponentsInChildren<GazeButton>(false).Any(b => b.gameObject.activeInHierarchy && b.Interactable && b.Label != null && match(b.Label.text));

        static void Tick()
        {
            if (!SessionState.GetBool(Key + "active", false) || ending) return;
            try
            {
                double start = double.Parse(SessionState.GetString(Key + "start", "0"), CultureInfo.InvariantCulture);
                if (EditorApplication.timeSinceStartup - start > 120) throw new TimeoutException("Watchdog de QA: 120 s.");
                if (!EditorApplication.isPlaying || result == null) return;
                guestA?.Pump(Time.unscaledTime);
                guestB?.Pump(Time.unscaledTime);
                PublishGuestPoses();
                if (runtimeErrors.Count > 0) throw new InvalidOperationException("Unity emitió un error durante la ejecución; revisar runtimeErrors.");
                switch (step)
                {
                    case 0:
                        if (!WaitFor(After(0.25) && SceneManager.GetActiveScene().name == LabState.TableScene &&
                            Find<PeriodicTableView>()?.tiles.Count == 118 && VRRig.Instance != null && VRRig.Instance.head != null &&
                            Find<LearningMissionBoard>() != null && Find<LanClassroom>() != null &&
                            Find<ScientificLibrary>() != null && HasButton(Find<ScientificLibrary>().transform, s => s == "Fuentes científicas") &&
                            HasButton(Find<LearningMissionBoard>().transform, s => s == "Continuar misión"), "Los paneles iniciales no quedaron listos en 15 s")) return;
                        Assert(SceneManager.GetActiveScene().name == LabState.TableScene, "Escena inicial de tabla");
                        Assert(Find<PeriodicTableView>()?.tiles.Count == 118, "118 casillas activas");
                        Assert(Find<LearningMissionBoard>() != null, "Panel de misiones creado en runtime");
                        Assert(Find<ScientificLibrary>() != null && Find<LanClassroom>() != null, "Biblioteca y acceso LAN inicializados");
                        Assert(LearningProgress.TotalPoints == 0 && LearningProgress.GetAttempts(26) == 0, "Perfil temporal de QA vacío");
                        Click(Find<ScientificLibrary>().transform, s => s == "Fuentes científicas");
                        Assert(ScientificLibrary.HandlesBackNavigation && GameObject.Find("BibliotecaCientifica") != null, "Biblioteca abre y captura navegación atrás");
                        Shot("01_fuentes", VRRig.Instance.head.position, VRRig.Instance.head.rotation);
                        Next(); break;
                    case 1:
                        if (!After(0.2)) return;
                        Click(Find<ScientificLibrary>().transform, s => s == "Siguiente");
                        Assert(Find<ScientificLibrary>().GetComponentsInChildren<TMP_Text>(false).Any(t => t.text.Contains(ScientificLibrary.Entries[1].Citation)), "Biblioteca cambia de referencia");
                        Click(Find<ScientificLibrary>().transform, s => s == "Cerrar");
                        Assert(ScientificLibrary.HandlesBackNavigation, "Cerrar fuente consume el frame de navegación");
                        closedOnFrame = Time.frameCount;
                        Next(); break;
                    case 2:
                        if (Time.frameCount <= closedOnFrame) { if (After(15)) throw new TimeoutException("Unity no avanza frames después de cerrar fuentes."); return; }
                        if (!After(0.15)) return;
                        Assert(!ScientificLibrary.HandlesBackNavigation, "Cerrar fuente libera navegación al frame siguiente");
                        var launcher = GameObject.Find("AccesoAulaLAN");
                        Assert(launcher != null, "Acceso LAN presente en la tabla");
                        Click(launcher.transform, s => s.StartsWith("Aula LAN"));
                        Assert(GameObject.Find("PanelAulaLAN") != null && LanClassroom.HandlesBackNavigation, "Panel LAN abre y captura navegación atrás");
                        Shot("02_lan", VRRig.Instance.head.position, VRRig.Instance.head.rotation);
                        Click(GameObject.Find("PanelAulaLAN").transform, s => s == "Cerrar panel");
                        Assert(LanClassroom.HandlesBackNavigation, "Cerrar LAN consume el frame de navegación");
                        closedOnFrame = Time.frameCount;
                        Next(); break;
                    case 3:
                        if (Time.frameCount <= closedOnFrame) { if (After(15)) throw new TimeoutException("Unity no avanza frames después de cerrar LAN."); return; }
                        if (!After(0.15)) return;
                        Assert(!LanClassroom.HandlesBackNavigation, "Cerrar LAN libera navegación al frame siguiente");
                        LabInput.InvertRightStickY = true;
                        Assert(PlayerPrefs.GetInt(InputKey) == 1, "Invertir eje guarda la preferencia nativa");
                        LabInput.InvertRightStickY = false;
                        Assert(PlayerPrefs.GetInt(InputKey) == 0, "Restablecer eje guarda la preferencia nativa");
                        ShotPanel("03_misiones", Find<LearningMissionBoard>().transform, 4.2f);
                        LabState.OpenElement(26); Next(); break;
                    case 4:
                        if (SceneManager.GetActiveScene().name != LabState.ElementScene || ScreenFader.Busy || ElementIntro.Active || !After(0.8)) { RequireScene(LabState.ElementScene); return; }
                        Assert(Find<ElementDetailController>() != null && LabState.SelectedZ == 26, "Entrada al nivel real de Fe y fin de introducción");
                        Assert(Find<AtomModel>()?.Mode == AtomViewMode.Bohr, "Modelo Bohr inicial");
                        Shot("04_bohr", new Vector3(0, 1.9f, 3.2f), Quaternion.identity);
                        Find<ElementDetailController>().SetMode(AtomViewMode.Cloud); Next(); break;
                    case 5:
                        if (!After(0.3)) return;
                        Assert(Find<AtomModel>()?.Mode == AtomViewMode.Cloud, "Cambio real a nube electrónica");
                        Assert(Find<AtomModel>().transform.Find("NubeElectronica").gameObject.activeInHierarchy, "Geometría de nube activa");
                        Shot("05_nube", new Vector3(0, 1.9f, 3.2f), Quaternion.identity);
                        Find<ElementDetailController>().SetMode(AtomViewMode.Nucleus); Next(); break;
                    case 6:
                        if (!After(0.3)) return;
                        Assert(Find<AtomModel>()?.Mode == AtomViewMode.Nucleus, "Cambio real a núcleo");
                        Shot("06_nucleo", new Vector3(0, 1.9f, 3.2f), Quaternion.identity);
                        Find<ElementDetailController>().SetMode(AtomViewMode.Bohr);
                        quiz = ChallengeCatalog.Generate(ElementDatabase.Get(26), LearningProgress.GetAttempts(26));
                        questionIndex = 0; Next(); break;
                    case 7:
                        if (!After(0.15)) return;
                        CompleteQuestion(); break;
                    case 8:
                        Assert(LearningProgress.GetAttempts(26) == 2 && LearningProgress.GetStars(26) == 3, "Dos intentos guardados, mejor marca intacta");
                        Assert(LearningProgress.TotalPoints == firstScore, "Repetir perfecto no produce farming");
                        var stored = JsonUtility.FromJson<LearningRecordStore>(PlayerPrefs.GetString(LearningKey));
                        Assert(stored.Find(26).Attempts == 2 && stored.TotalPoints == firstScore, "JSON nativo PlayerPrefs contiene resultado real");
                        typeof(LearningProgress).GetField("store", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, null);
                        Assert(LearningProgress.TotalPoints == firstScore && LearningProgress.GetAttempts(26) == 2, "Recargar desde PlayerPrefs recupera puntos e intentos");
                        LabState.BackToTable(); Next(); break;
                    case 9:
                        if (SceneManager.GetActiveScene().name != LabState.TableScene || ScreenFader.Busy || !After(0.7)) { RequireScene(LabState.TableScene); return; }
                        Assert(Find<LearningMissionBoard>() != null && LearningProgress.GetStars(26) == 3, "Vuelta a tabla reconstruye panel y conserva logro");
                        LabState.OpenElement(26); Next(); break;
                    case 10:
                        if (SceneManager.GetActiveScene().name != LabState.ElementScene || ScreenFader.Busy || ElementIntro.Active || !After(0.8)) { RequireScene(LabState.ElementScene); return; }
                        Assert(Find<ElementChallengeStation>() != null && LearningProgress.GetAttempts(26) == 2 && LearningProgress.TotalPoints == firstScore, "Reabrir Fe no reinicia ni duplica el reto");
                        Find<ReactionStation>().Run(8); Next(); break;
                    case 11:
                        var station = Find<ReactionStation>();
                        Assert(station != null, "Estación de reacciones presente");
                        if (Field<Coroutine>(station, "running") != null || !After(0.5))
                        { if (After(15)) throw new TimeoutException("La reacción Fe/O no terminó dentro de 15 s."); return; }
                        var expected = ReactionDatabase.React(ElementDatabase.Get(26), ElementDatabase.Get(8));
                        Assert(Field<TextMeshPro>(station, "equationText").text == ReactionDatabase.FormatEquation(expected.Equation), "Reacción muestra ecuación esperada al terminar la corutina");
                        Assert(!string.IsNullOrEmpty(Field<TextMeshPro>(station, "descriptionText").text), "Reacción muestra explicación");
                        ShotPanel("08_reaccion", station.transform, 3.6f);
                        LabState.BackToTable(); Next(); break;
                    case 12:
                        if (SceneManager.GetActiveScene().name != LabState.TableScene || ScreenFader.Busy || !After(0.7)) { RequireScene(LabState.TableScene); return; }
                        Assert(Find<LearningMissionBoard>() != null && LearningProgress.TotalPoints == firstScore, "Tabla final conserva progreso tras segunda transición");
                        Shot("09_tabla_final", VRRig.Instance.head.position, VRRig.Instance.head.rotation);
                        Click(GameObject.Find("AccesoAulaLAN").transform, s => s.StartsWith("Aula LAN"));
                        Click(LanClassroom.ActivePanel, s => s == "Crear aula");
                        hostSession = Field<LanSession>(Find<LanClassroom>(), "session");
                        Assert(hostSession != null && hostSession.IsHost && hostSession.Connected, "La GUI crea un anfitrión TCP real");
                        Assert(LanClassroom.ActivePanel.TransformPoint(new Vector3(0, -1.9f, 0)).y > 0.05f,
                            "El panel LAN y sus controles quedan por encima del suelo");
                        guestA = new LanSession(); guestB = new LanSession();
                        guestA.Join("QA Visitante 1", "127.0.0.1", hostSession.Port);
                        guestB.Join("QA Visitante 2", "127.0.0.1", hostSession.Port);
                        Next(); break;
                    case 13:
                        if (!WaitFor(guestA.Connected && guestB.Connected && hostSession.State.members.Length == 3 &&
                            guestA.State.members.Length == 3 && guestB.State.members.Length == 3, "No se unieron ambos clientes TCP al anfitrión de la GUI")) return;
                        Assert(guestA.SelfId != guestB.SelfId && guestA.SelfId != hostSession.SelfId, "Tres identidades asignadas por el servidor real");
                        guestA.SetReady(true); guestB.SetReady(true);
                        result.lanParticipants = 3;
                        Next(); break;
                    case 14:
                        if (!WaitFor(hostSession.State.members.All(m => m.ready) &&
                            HasButton(LanClassroom.ActivePanel, s => s.StartsWith("Iniciar:")) && AvatarsReady(), "El lobby no reflejó Listo y poses de los dos clientes")) return;
                        Assert(hostSession.State.members.Count(m => m.hasPose) == 3, "Las tres poses viajan por el protocolo de sesión");
                        Assert(Field<Dictionary<string, Transform>>(Find<LanClassroom>(), "avatars").Values.All(a =>
                            a.GetComponentsInChildren<Renderer>().All(r => !r.enabled)), "Avatares ocultos mientras el panel LAN está abierto");
                        Assert(LanClassroom.ActivePanel.GetComponentsInChildren<TMP_Text>(false).Any(t => t.text.Contains("QA Visitante 1") && t.text.Contains("QA Visitante 2")), "La GUI muestra ambos participantes reales del lobby");
                        ShotPanel("10_lan_lobby_listo", LanClassroom.ActivePanel, 4.0f);
                        Click(LanClassroom.ActivePanel, s => s == "Cerrar panel");
                        closedOnFrame = Time.frameCount;
                        Next(); break;
                    case 15:
                        if (!WaitFor(Time.frameCount > closedOnFrame && AvatarsReady(), "No se pudieron observar los avatares tras cerrar el lobby")) return;
                        Assert(!LanClassroom.HandlesBackNavigation, "Cerrar lobby no abandona la sesión anfitriona");
                        Assert(Field<Dictionary<string, Transform>>(Find<LanClassroom>(), "avatars").Values.All(a =>
                            a.GetComponentsInChildren<Renderer>().All(r => r.enabled)), "Cerrar el panel vuelve a mostrar los avatares");
                        Shot("11_lan_avatares_tabla", VRRig.Instance.head.position, VRRig.Instance.head.rotation);
                        Click(GameObject.Find("AccesoAulaLAN").transform, s => s.StartsWith("Aula LAN"));
                        Click(LanClassroom.ActivePanel, s => s.StartsWith("Iniciar:"));
                        Assert(hostSession.State.phase == "quiz", "El botón de la GUI inicia una ronda autoritativa");
                        lanQuestion = 0;
                        Next(); break;
                    case 16:
                        if (!WaitFor(SceneManager.GetActiveScene().name == LabState.ElementScene && !ScreenFader.Busy && !ElementIntro.Active &&
                            hostSession.State.phase == "quiz" && guestA.State.phase == "quiz" && guestB.State.phase == "quiz" &&
                            guestA.State.worldScene == LabState.ElementScene && guestB.State.worldScene == LabState.ElementScene && After(0.5),
                            "El inicio LAN no sincronizó el laboratorio real de Fe")) return;
                        Assert(LabState.SelectedZ == 26 && hostSession.State.element == 26 && guestA.State.element == 26 && guestB.State.element == 26,
                            "La ronda sincroniza Fe en anfitrión y ambos clientes TCP");
                        if (LanClassroom.ActivePanel == null) Click(GameObject.Find("AccesoAulaLAN").transform, s => s.StartsWith("Aula LAN"));
                        lanPanelFrame = Time.frameCount;
                        Next(); break;
                    case 17:
                        if (!WaitFor(Time.frameCount > lanPanelFrame && After(0.15) && hostSession.State.step == lanQuestion && guestA.State.step == lanQuestion && guestB.State.step == lanQuestion &&
                            !hostSession.State.revealed && AvatarsReady() && LanClassroom.ActivePanel != null &&
                            LanClassroom.ActivePanel.GetComponentsInChildren<TMP_Text>(false).Any(t => t.text.Contains(hostSession.State.question)),
                            "La GUI no mostró la pregunta LAN sincronizada")) return;
                        int correct = LanCorrectChoice();
                        Assert(correct >= 0 && hostSession.State.options.Distinct().Count() == 3, "Opciones reales de LAN contienen respuesta correcta y son únicas");
                        Assert(Field<Dictionary<string, Transform>>(Find<LanClassroom>(), "avatars").Values.All(a =>
                            a.GetComponentsInChildren<Renderer>().All(r => !r.enabled)) &&
                            !Find<ScientificLibrary>().GetComponentsInChildren<GazeButton>(true).Single(b => b.Label.text == "Fuentes científicas").gameObject.activeInHierarchy,
                            "Pregunta LAN legible sin avatares ni acceso de fuentes delante del panel");
                        if (lanQuestion == 0) ShotPanel("12_lan_pregunta", LanClassroom.ActivePanel, 4.0f);
                        Click(LanClassroom.ActivePanel, s => s == ((char)('A' + correct)) + ") " + hostSession.State.options[correct]);
                        guestA.Answer(correct); guestB.Answer((correct + 1) % 3);
                        Next(); break;
                    case 18:
                        if (!WaitFor(hostSession.State.revealed && guestA.State.revealed && guestB.State.revealed &&
                            HasButton(LanClassroom.ActivePanel, s => s == (lanQuestion == 2 ? "Ver resultado" : "Siguiente pregunta")) &&
                            LanClassroom.ActivePanel.GetComponentsInChildren<TMP_Text>(false).Any(t => t.text == hostSession.State.explanation),
                            "El feedback LAN no llegó a la GUI y a ambos clientes")) return;
                        Assert(hostSession.Self.points == (lanQuestion + 1) * 100 && guestA.Self.points == (lanQuestion + 1) * 100 && guestB.Self.points == 0,
                            "Puntos LAN reflejan respuestas reales correctas e incorrectas");
                        if (lanQuestion == 0) ShotPanel("13_lan_feedback", LanClassroom.ActivePanel, 4.0f);
                        Click(LanClassroom.ActivePanel, s => s == (lanQuestion == 2 ? "Ver resultado" : "Siguiente pregunta"));
                        Next(); break;
                    case 19:
                        if (lanQuestion < 2)
                        {
                            if (!WaitFor(hostSession.State.step == lanQuestion + 1 && guestA.State.step == lanQuestion + 1 && guestB.State.step == lanQuestion + 1,
                                "El anfitrión no avanzó la pregunta para ambos clientes")) return;
                            lanQuestion++;
                            Next(17); return;
                        }
                        if (!WaitFor(hostSession.State.phase == "summary" && guestA.State.phase == "summary" && guestB.State.phase == "summary" &&
                            HasButton(LanClassroom.ActivePanel, s => s == "Preparar otra ronda"), "No llegó el resumen de la ronda LAN")) return;
                        Assert(hostSession.Self.correct == 3 && guestA.Self.correct == 3 && guestB.Self.correct == 0, "El resumen registra tres respuestas por participante");
                        Assert(LearningProgress.GetAttempts(26) == 3 && LearningProgress.TotalPoints == firstScore, "Ronda LAN registra un intento sin duplicar mejor marca ni puntos");
                        result.lanQuestionsCompleted = 3;
                        ShotPanel("14_lan_resultado", LanClassroom.ActivePanel, 4.0f);
                        Click(LanClassroom.ActivePanel, s => s == "Cerrar aula para todos");
                        Next(); break;
                    case 20:
                        if (!WaitFor(!LanClassroom.HasActiveSession && !hostSession.Connected && !guestA.Connected && !guestB.Connected,
                            "Cerrar aula no desconectó a los dos clientes reales")) return;
                        Assert(!LanClassroom.HasActiveSession, "El anfitrión de la GUI cierra el aula y sus conexiones");
                        if (LanClassroom.ActivePanel != null) Click(LanClassroom.ActivePanel, s => s == "Cerrar panel");
                        Assert(string.IsNullOrEmpty(CleanupScenario()), "Las conexiones TCP de QA se liberan sin errores");
                        LabState.BackToTable();
                        Next(); break;
                    case 21:
                        if (SceneManager.GetActiveScene().name != LabState.TableScene || ScreenFader.Busy || !After(0.7)) { RequireScene(LabState.TableScene); return; }
                        Assert(LearningProgress.TotalPoints == firstScore && LearningProgress.GetAttempts(26) == 3 && !LanClassroom.HasActiveSession,
                            "Salir del multijugador conserva la práctica local y permite volver a la tabla");
                        ExportNativeCsv();
                        ShotPanel("15_informe_progreso", Find<LearningMissionBoard>().transform, 4.2f);
                        Finish(null); break;
                }
            }
            catch (Exception ex) { Finish(ex.ToString()); }
        }

        static void CompleteQuestion()
        {
            var station = Find<ElementChallengeStation>();
            Assert(station != null, "Reto visible al responder");
            var question = quiz[questionIndex];
            Assert(station.GetComponentsInChildren<TMP_Text>(false).Any(t => t.text == question.Question), "Enunciado corresponde a la variante guardada");
            int mistakes = Field<int>(station, "totalMistakes");
            station.Answer(-1); station.Answer(3);
            Assert(Field<int>(station, "totalMistakes") == mistakes, "Índices de respuesta inválidos son inertes");
            Button(station.transform, s => s.EndsWith(question.Options[question.CorrectIndex]));
            station.Answer(question.CorrectIndex);
            station.Answer(question.CorrectIndex);
            Assert(Field<bool>(station, "stepSolved"), "Respuesta correcta resuelve pregunta una sola vez");
            Click(station.transform, s => s.StartsWith(questionIndex < 2 ? "Siguiente pregunta" : "Ver resultado"));
            questionIndex++;
            if (questionIndex < 3) { stepStart = EditorApplication.timeSinceStartup; return; }
            completedQuizzes++;
            Assert(LearningProgress.GetAttempts(26) == completedQuizzes, "Resumen registra exactamente un intento");
            if (completedQuizzes == 1)
            {
                firstScore = LearningProgress.TotalPoints;
                Assert(firstScore == 80 && LearningProgress.GetStars(26) == 3, "Primera exploración y mejor marca perfecta:80 puntos");
                ShotPanel("07_reto_completado", station.transform, 3.4f);
                Click(station.transform, s => s.StartsWith("Repasar con otra variante"));
                quiz = ChallengeCatalog.Generate(ElementDatabase.Get(26), LearningProgress.GetAttempts(26));
                questionIndex = 0;
                stepStart = EditorApplication.timeSinceStartup;
            }
            else Next();
        }

        static bool AvatarsReady()
        {
            var classroom = Find<LanClassroom>();
            if (classroom == null || guestA == null || guestB == null || !guestA.Connected || !guestB.Connected) return false;
            var avatars = Field<Dictionary<string, Transform>>(classroom, "avatars");
            return avatars.TryGetValue(guestA.SelfId, out var a) && a != null && a.gameObject.activeInHierarchy &&
                avatars.TryGetValue(guestB.SelfId, out var b) && b != null && b.gameObject.activeInHierarchy;
        }

        static void PublishGuestPoses()
        {
            if (hostSession == null || !hostSession.Connected || Time.unscaledTime < nextGuestPose || ScreenFader.Busy) return;
            string scene = SceneManager.GetActiveScene().name;
            if (scene != LabState.TableScene && scene != LabState.ElementScene) return;
            nextGuestPose = Time.unscaledTime + 0.25f;
            var guests = new[] { guestA, guestB };
            for (int i = 0; i < guests.Length; i++)
            {
                var guest = guests[i];
                if (guest == null || !guest.Connected || guest.State.worldScene != scene ||
                    (scene == LabState.ElementScene && guest.State.worldElement != LabState.SelectedZ)) continue;
                // Entradas de clientes de prueba por la API pública, enviadas mediante TCP real.
                // No se asignan poses, miembros, ready, opciones ni puntos directamente al estado.
                guest.PublishPose(i == 0 ? -1.2f : 1.2f, 0f, scene == LabState.TableScene ? 4.1f : 2.7f, 180f, scene, LabState.SelectedZ);
            }
        }

        static int LanCorrectChoice()
        {
            var e = ElementDatabase.Get(hostSession.State.element);
            string answer = hostSession.State.step == 0 ? e.Z.ToString() :
                hostSession.State.step == 1 ? CategoryInfo.Name(e.Category) : e.Period.ToString();
            return Array.IndexOf(hostSession.State.options, answer);
        }

        static void ExportNativeCsv()
        {
            nativeCsv = LearningProgress.ExportCsv();
            Assert(File.Exists(nativeCsv), "Exportación nativa crea un archivo CSV real sin abrir selector externo");
            var rows = File.ReadAllLines(nativeCsv).Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
            Assert(rows.Length == 119 && rows.All(row => row.Split(';').Length == 11), "CSV exportado contiene cabecera y 118 filas de once columnas");
            var fe = rows.Skip(1).Select(row => row.Split(';')).Single(row => row[0] == "26");
            Assert(fe[1] == "Fe" && fe[3] == "3" && fe[4] == "3" && fe[8] == "3", "CSV nativo refleja intentos y mejor marca de Fe después del modo LAN");
            result.csvArtifact = Path.Combine(result.captureDirectory, "informe_QA.csv");
            File.Copy(nativeCsv, result.csvArtifact, true);
            result.nativeCsvVerified = true;
        }

        static string CleanupScenario()
        {
            string errors = "";
            void Close(Action action)
            {
                try { action(); }
                catch (Exception ex) { errors += ex.GetType().Name + ":" + ex.Message + "\n"; }
            }
            Close(() => guestA?.Dispose());
            Close(() => guestB?.Dispose());
            var classroom = Find<LanClassroom>();
            if (classroom != null && hostSession != null)
            {
                Close(() =>
                {
                    if (ReferenceEquals(Field<LanSession>(classroom, "session"), hostSession))
                        classroom.GetType().GetMethod("Disconnect", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(classroom, null);
                });
            }
            Close(() => hostSession?.Dispose());
            guestA = guestB = hostSession = null;
            if (!string.IsNullOrEmpty(nativeCsv))
            {
                Close(() =>
                {
                    string ownedDirectory = Path.GetFullPath(Path.Combine(Application.persistentDataPath, "Informes")) + Path.DirectorySeparatorChar;
                    string ownedFile = Path.GetFullPath(nativeCsv);
                    if (!ownedFile.StartsWith(ownedDirectory, StringComparison.OrdinalIgnoreCase)) throw new IOException("CSV de QA fuera de su carpeta prevista.");
                    // Se conserva la copia de evidencia; solo se retira el archivo temporal creado por esta prueba.
                    if (File.Exists(ownedFile)) File.Delete(ownedFile);
                });
                nativeCsv = null;
            }
            if (result != null) result.testSocketsClosed = string.IsNullOrEmpty(errors);
            return errors;
        }

        static void ShotPanel(string name, Transform panel, float distance) =>
            Shot(name, panel.position - panel.forward * distance, panel.rotation);

        static void Shot(string name, Vector3 position, Quaternion rotation)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) return;
            foreach (var text in Object.FindObjectsByType<TextMeshPro>())
                text.ForceMeshUpdate(true, true);
            var cameraObject = new GameObject("CamaraQA");
            var camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.transform.SetPositionAndRotation(position, rotation);
            camera.fieldOfView = 65; camera.nearClipPlane = 0.05f; camera.farClipPlane = 100;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.gray;
            camera.allowHDR = false;
            var render = RenderTexture.GetTemporary(1280, 720, 24);
            var previous = RenderTexture.active;
            Texture2D image = null;
            try
            {
                camera.targetTexture = render;
                camera.Render();
                RenderTexture.active = render;
                image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                image.Apply();
                string path = Path.Combine(result.captureDirectory, name + ".png");
                File.WriteAllBytes(path, image.EncodeToPNG());
                Assert(new FileInfo(path).Length > 0, "Captura PNG generada:" + name);
                screenshots.Add(path);
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(render);
                if (image != null) Object.DestroyImmediate(image);
                Object.DestroyImmediate(cameraObject);
            }
        }

        static void Finish(string error, bool requestExit = true)
        {
            if (ending) return;
            ending = true;
            SessionState.SetBool(Key + "ending", true);
            if (result == null) result = new Result { startedUtc = SessionState.GetString(Key + "utc", "") };
            result.error = error;
            string cleanup = CleanupScenario();
            if (!string.IsNullOrEmpty(cleanup)) result.error += "\nLimpieza del escenario:" + cleanup;
            result.success = string.IsNullOrEmpty(result.error) && runtimeErrors.Count == 0;
            result.finishedUtc = DateTime.UtcNow.ToString("o");
            result.checks = assertions.Count; result.assertionExecutions = assertionExecutions; result.lastStep = step;
            result.assertions = assertions.ToArray(); result.runtimeErrors = runtimeErrors.ToArray(); result.screenshots = screenshots.ToArray();
            PersistResult();
            EditorApplication.update -= Tick;
            Application.logMessageReceived -= Log;
            if (EditorApplication.isPlaying) { if (requestExit) EditorApplication.ExitPlaymode(); }
            else FinalizeInEditMode();
        }

        static void FinalizeInEditMode()
        {
            if (!SessionState.GetBool(Key + "active", false)) return;
            bool batch = SessionState.GetBool(Key + "batch", false);
            string saved = SessionState.GetString(Key + "report", "");
            result = !string.IsNullOrEmpty(saved) ? JsonUtility.FromJson<Result>(saved) : new Result();
            try
            {
                RestorePreferences();
                result.preferencesRestored = true;
                if (!batch)
                {
                    var scenes = JsonUtility.FromJson<SceneBackup>(SessionState.GetString(Key + "scenes", ""));
                    if (scenes?.setup != null) EditorSceneManager.RestoreSceneManagerSetup(scenes.setup);
                }
            }
            catch (Exception ex) { result.success = false; result.error += "\nRestauración QA:" + ex; }
            PersistResult();
            SessionState.SetBool(Key + "active", false);
            SessionState.SetBool(Key + "ending", false);
            EditorApplication.update -= Tick;
            Application.logMessageReceived -= Log;
            if (batch) EditorApplication.Exit(result.success ? 0 : 1);
            else Debug.Log("[Runtime QA] " + (result.success ? "OK" : "FALLO") + ": " + result.checks + " comprobaciones. Ver " + ReportPath);
        }

        static void PersistResult()
        {
            string json = JsonUtility.ToJson(result, true);
            SessionState.SetString(Key + "report", json);
            try { File.WriteAllText(ReportPath, json); }
            catch (Exception ex)
            {
                // Un disco lleno no debe impedir salir de Play y restaurar las preferencias.
                result.success = false;
                result.error += "\nNo se pudo escribir runtime.json: " + ex.Message;
                SessionState.SetString(Key + "report", JsonUtility.ToJson(result, true));
            }
        }
    }
}

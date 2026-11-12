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
            public bool executed, success, preferencesRestored;
            public string startedUtc, finishedUtc, error, captureDirectory;
            public int checks, lastStep;
            public string scope = "Unity Editor Play: UI por callbacks, transiciones reales, persistencia nativa, modelos y reacción";
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
        static readonly List<string> runtimeErrors = new List<string>();
        static readonly List<string> screenshots = new List<string>();
        static int step, questionIndex, completedQuizzes, firstScore, closedOnFrame;
        static double stepStart;
        static bool ending;
        static List<ChallengeQuestion> quiz;

        static LabRuntimeValidation()
        {
            EditorApplication.playModeStateChanged += PlayModeChanged;
            if (SessionState.GetBool(Key + "active", false)) Attach();
        }

        [MenuItem("Quimikaze/Validar flujo real en Play (QA)", priority = 83)]
        public static void RunMenu() => Begin(false);

        public static void RunBatch() => Begin(true);

        static void Begin(bool batch)
        {
            if (SessionState.GetBool(Key + "active", false)) throw new InvalidOperationException("Ya existe una prueba Play en curso.");
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Sal de Play antes de iniciar QA.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Guarda tus escenas modificadas antes de ejecutar QA.");
            Directory.CreateDirectory(Dir);
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
            assertions.Clear(); runtimeErrors.Clear(); screenshots.Clear();
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
                assertions.Clear(); screenshots.Clear();
                step = questionIndex = completedQuizzes = firstScore = 0;
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
            if (!condition) throw new InvalidOperationException("Play QA: " + description);
            assertions.Add(description);
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

        static void Tick()
        {
            if (!SessionState.GetBool(Key + "active", false) || ending) return;
            try
            {
                double start = double.Parse(SessionState.GetString(Key + "start", "0"), CultureInfo.InvariantCulture);
                if (EditorApplication.timeSinceStartup - start > 120) throw new TimeoutException("Watchdog de QA: 120 s.");
                if (!EditorApplication.isPlaying || result == null) return;
                if (runtimeErrors.Count > 0) throw new InvalidOperationException("Unity emitió un error durante la ejecución; revisar runtimeErrors.");
                switch (step)
                {
                    case 0:
                        if (!After(1)) return;
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

        static void ShotPanel(string name, Transform panel, float distance) =>
            Shot(name, panel.position - panel.forward * distance, panel.rotation);

        static void Shot(string name, Vector3 position, Quaternion rotation)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) return;
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
            result.success = error == null && runtimeErrors.Count == 0;
            result.finishedUtc = DateTime.UtcNow.ToString("o");
            result.checks = assertions.Count; result.lastStep = step;
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

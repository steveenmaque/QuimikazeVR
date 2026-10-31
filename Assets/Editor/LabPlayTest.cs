using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LabQuimicaVR.EditorTools
{
    /// <summary>
    /// Prueba automatica en modo Play: recorre la tabla, entra a un elemento, cambia los modelos,
    /// hace una reaccion y guarda capturas y errores en AutoSync/prueba.
    /// </summary>
    [InitializeOnLoad]
    public static class LabPlayTest
    {
        const string Dir = "AutoSync/prueba";
        const string Key = "LabPlayTest.Running";
        static double t0;
        static int step;
        static readonly List<string> log = new List<string>();

        static LabPlayTest()
        {
            if (Application.isBatchMode) return;
            EditorApplication.playModeStateChanged += OnPlayMode;
        }

        [MenuItem("Quimikaze/Probar en modo Play", priority = 41)]
        public static void Run()
        {
            Directory.CreateDirectory(Dir);
            foreach (var f in Directory.GetFiles(Dir)) File.Delete(f);
            EditorSceneManager.OpenScene(LabSceneBuilder.TableScenePath);
            SessionState.SetBool(Key, true);
            EditorApplication.EnterPlaymode();
        }

        static void OnPlayMode(PlayModeStateChange s)
        {
            if (!SessionState.GetBool(Key, false)) return;
            if (s == PlayModeStateChange.EnteredPlayMode)
            {
                step = 0;
                t0 = EditorApplication.timeSinceStartup;
                log.Clear();
                Application.logMessageReceived += OnLog;
                EditorApplication.update += Tick;
            }
            else if (s == PlayModeStateChange.ExitingPlayMode)
            {
                EditorApplication.update -= Tick;
                Application.logMessageReceived -= OnLog;
                File.WriteAllLines(Path.Combine(Dir, "log.txt"), log);
                SessionState.SetBool(Key, false);
                LabAutoSync.WriteStatus("Prueba en Play terminada");
            }
        }

        static void OnLog(string msg, string stack, LogType type)
        {
            if (type == LogType.Log) return;
            log.Add($"[{type}] {msg}\n{stack}");
        }

        static bool After(double seconds) => EditorApplication.timeSinceStartup - t0 > seconds;

        static void Next()
        {
            log.Add($"[Paso {step}] frame {Time.frameCount}, escena {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
            step++;
            t0 = EditorApplication.timeSinceStartup;
        }

        static void Tick()
        {
            if (!EditorApplication.isPlaying) return;
            switch (step)
            {
                case 0:
                    if (!After(2.5)) return;
                    ShotFromRig("1_tabla_inicio");
                    Object.FindFirstObjectByType<PeriodicTableView>()?.ShowPreview(26);
                    Next();
                    break;
                case 1:
                    if (!After(0.5)) return;
                    ShotFromRig("2_tabla_ficha_hierro");
                    LabState.OpenElement(26);
                    Next();
                    break;
                case 2:
                    if (!After(2.4)) return;
                    ShotFromRig("3a_transicion_paisaje");
                    Next();
                    break;
                case 3:
                    // Espera a que termine la transicion con el paisaje
                    if (ElementIntro.Active && !After(8.0)) return;
                    if (!After(0.8)) return;
                    ShotFromRig("3_hierro_bohr");
                    Shot("3b_hierro_vitrina", new Vector3(1.4f, 1.65f, 1.3f), new Vector3(10, 38, 0));
                    Shot("3c_hierro_descubridores", new Vector3(-0.6f, 1.8f, -3.2f), new Vector3(-4, -90, 0));
                    Shot("3d_hierro_naturaleza", new Vector3(0.6f, 1.8f, -3.2f), new Vector3(-4, 90, 0));
                    Shot("4_hierro_bohr_cerca", new Vector3(0, 1.9f, 3.2f), new Vector3(0, 0, 0));
                    Object.FindFirstObjectByType<ElementDetailController>()?.SetMode(AtomViewMode.Cloud);
                    Next();
                    break;
                case 4:
                    if (!After(1.5)) return;
                    Shot("5_hierro_nube", new Vector3(0, 1.9f, 3.2f), Vector3.zero);
                    Object.FindFirstObjectByType<ElementDetailController>()?.SetMode(AtomViewMode.Nucleus);
                    Next();
                    break;
                case 5:
                    if (!After(1.5)) return;
                    Shot("6_hierro_nucleo", new Vector3(0, 1.9f, 3.2f), Vector3.zero);
                    Object.FindFirstObjectByType<ReactionStation>()?.Run(8);
                    Next();
                    break;
                case 6:
                    if (!After(1.35)) return;
                    ShotReaction("7_reaccion_efecto");
                    Next();
                    break;
                case 7:
                    if (!After(2.5)) return;
                    ShotReaction("8_reaccion_resultado");
                    Object.FindFirstObjectByType<ReactionStation>()?.Run(17);
                    Next();
                    break;
                case 8:
                    if (!After(4.0)) return;
                    ShotReaction("9_reaccion_cloro");
                    LabState.OpenElement(1);
                    Next();
                    break;
                case 9:
                    if (!After(6.5)) return;
                    ShotFromRig("10_hidrogeno");
                    Object.FindFirstObjectByType<ElementDetailController>()?.SetMode(AtomViewMode.Cloud);
                    Next();
                    break;
                case 10:
                    if (!After(1.5)) return;
                    Shot("11_hidrogeno_nube", new Vector3(0, 1.9f, 3.2f), Vector3.zero);
                    LabState.OpenElement(92);
                    Next();
                    break;
                case 11:
                    if (!After(6.5)) return;
                    ShotFromRig("12_uranio");
                    Shot("13_uranio_cerca", new Vector3(0, 1.9f, 3.2f), Vector3.zero);
                    LabState.BackToTable();
                    Next();
                    break;
                case 12:
                    if (!After(2.5)) return;
                    ShotFromRig("14_tabla_regreso");
                    Next();
                    EditorApplication.ExitPlaymode();
                    break;
            }
        }

        static void ShotFromRig(string name)
        {
            var rig = Object.FindFirstObjectByType<VRRig>();
            if (rig == null) { log.Add("No se encontró el JugadorVR para la captura " + name); return; }
            Shot(name, rig.head.position, rig.head.eulerAngles);
        }

        static void ShotReaction(string name)
        {
            var station = Object.FindFirstObjectByType<ReactionStation>();
            if (station == null) return;
            var t = station.transform;
            var pos = t.position - t.forward * 2.6f + Vector3.down * 0.5f;
            Shot(name, pos, Quaternion.LookRotation(t.position + Vector3.down * 0.55f - pos).eulerAngles);
        }

        static void Shot(string name, Vector3 pos, Vector3 euler)
        {
            var go = new GameObject("CapturaPrueba");
            var cam = go.AddComponent<Camera>();
            cam.transform.SetPositionAndRotation(pos, Quaternion.Euler(euler));
            cam.fieldOfView = 70f;
            cam.nearClipPlane = 0.05f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.gray;
            const int w = 1280, h = 720;
            var rt = new RenderTexture(w, h, 24) { antiAliasing = 4 };
            cam.targetTexture = rt;
            cam.Render();
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            File.WriteAllBytes(Path.Combine(Dir, name + ".jpg"), tex.EncodeToJPG(85));
            cam.targetTexture = null;
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(go);
        }
    }
}

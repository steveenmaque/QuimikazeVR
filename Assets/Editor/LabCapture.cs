using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LabQuimicaVR.EditorTools
{
    /// <summary>Guarda imagenes de las escenas desde el punto de vista del jugador (AutoSync/capturas).</summary>
    public static class LabCapture
    {
        const string Dir = "AutoSync/capturas";

        [MenuItem("Quimikaze/Capturar vistas", priority = 40)]
        public static void CaptureAll()
        {
            Directory.CreateDirectory(Dir);
            var current = EditorSceneManager.GetActiveScene().path;

            EditorSceneManager.OpenScene(LabSceneBuilder.TableScenePath);
            Prepare();
            Shot("tabla_frente", new Vector3(0, 1.6f, 1.5f), new Vector3(-8, 0, 0), 75);
            Shot("tabla_cerca", new Vector3(-1.5f, 2.6f, 7.5f), new Vector3(-5, 0, 0), 65);
            Shot("tabla_izquierda", new Vector3(0, 1.6f, 3f), new Vector3(-5, -90, 0), 80);
            Shot("tabla_derecha", new Vector3(0, 1.6f, 3f), new Vector3(-5, 90, 0), 80);
            Shot("tabla_atras", new Vector3(0, 1.8f, 3f), new Vector3(-8, 180, 0), 80);
            Shot("tabla_general", new Vector3(10.5f, 6.5f, -5.5f), new Vector3(25, -40, 0), 80);

            EditorSceneManager.OpenScene(LabSceneBuilder.ElementScenePath);
            Prepare();
            Shot("elemento_frente", new Vector3(0, 1.6f, 0.2f), new Vector3(-6, 0, 0), 80);
            Shot("elemento_izquierda", new Vector3(0.8f, 1.8f, 4f), new Vector3(-4, -90, 0), 80);
            Shot("elemento_derecha", new Vector3(-0.8f, 1.8f, 4f), new Vector3(-4, 90, 0), 80);
            Shot("elemento_reacciones", new Vector3(-1.6f, 1.7f, 5.0f), new Vector3(-4, -24, 0), 70);
            Shot("elemento_controles", new Vector3(1.6f, 1.6f, 5.0f), new Vector3(-2, 24, 0), 70);
            Shot("elemento_atras", new Vector3(0, 1.7f, 2f), new Vector3(-4, 180, 0), 75);
            Shot("elemento_general", new Vector3(4.2f, 4.6f, -3.4f), new Vector3(25, -30, 0), 80);
            Shot("elemento_descubridores", new Vector3(-0.6f, 1.8f, -3.2f), new Vector3(-4, -90, 0), 80);
            Shot("elemento_naturaleza", new Vector3(0.6f, 1.8f, -3.2f), new Vector3(-4, 90, 0), 80);
            Shot("elemento_ventana", new Vector3(0, 1.7f, -1.0f), new Vector3(-10, 180, 0), 85);
            FaceCards(new Vector3(1.5f, 1.6f, 1.2f));
            Shot("elemento_vitrina", new Vector3(1.5f, 1.6f, 1.2f), new Vector3(8, 34, 0), 60);

            if (!string.IsNullOrEmpty(current)) EditorSceneManager.OpenScene(current);
            LabAutoSync.WriteStatus("Capturas guardadas en " + Dir);
        }

        /// <summary>
        /// Muestra por muestra (los 118 elementos), vista de cerca, para compararlas con las fotos reales.
        /// Se guardan en AutoSync/muestras.
        /// </summary>
        [MenuItem("Quimikaze/Capturar muestras (118)", priority = 42)]
        public static void CaptureSamples() => CaptureSamplesRange(1, 118);

        public static void CaptureSamplesRange(int from, int to)
        {
            const string dir = "AutoSync/muestras";
            Directory.CreateDirectory(dir);
            if (EditorSceneManager.GetActiveScene().path != LabSceneBuilder.ElementScenePath)
                EditorSceneManager.OpenScene(LabSceneBuilder.ElementScenePath);
            ElementExtras.Reload();
            var c = Object.FindFirstObjectByType<ElementDetailController>();
            if (c == null || c.sampleAnchor == null) { LabAutoSync.WriteStatus("ERROR: no hay ElementDetailController"); return; }
            var anchor = c.sampleAnchor;
            for (int z = from; z <= to; z++)
            {
                LabFactory.ClearChildren(anchor);
                ElementSample.Build(anchor, ElementDatabase.Get(z));
                var card = anchor.Find("Ficha");
                if (card != null) card.gameObject.SetActive(false);
                var target = anchor.position + new Vector3(0, 1.18f, 0);
                var camPos = target + new Vector3(-0.62f, 0.44f, -0.96f);
                Shot($"{z:000}", camPos, Quaternion.LookRotation(target - camPos).eulerAngles, 42, 640, 640, dir);
            }
            LabFactory.ClearChildren(anchor);
            c.Populate(c.previewZ);
            EditorSceneManager.MarkSceneDirty(c.gameObject.scene);
            LabAutoSync.WriteStatus("OK: muestras capturadas en " + dir);
        }

        /// <summary>Pantallas de descubridores y naturaleza de varios elementos (revision de retratos).</summary>
        [MenuItem("Quimikaze/Capturar descubridores", priority = 43)]
        public static void CaptureDiscoverers()
        {
            const string dir = "AutoSync/descubridores";
            Directory.CreateDirectory(dir);
            if (EditorSceneManager.GetActiveScene().path != LabSceneBuilder.ElementScenePath)
                EditorSceneManager.OpenScene(LabSceneBuilder.ElementScenePath);
            ElementExtras.Reload();
            var c = Object.FindFirstObjectByType<ElementDetailController>();
            foreach (int z in new[] { 1, 8, 18, 29, 40, 79, 88, 94, 113, 118 })
            {
                c.Populate(z);
                Prepare();
                Shot($"{z:000}_descubridores", new Vector3(-0.6f, 1.8f, -3.2f), new Vector3(-4, -90, 0), 80, 1280, 720, dir);
                Shot($"{z:000}_naturaleza", new Vector3(0.6f, 1.8f, -3.2f), new Vector3(-4, 90, 0), 80, 1280, 720, dir);
            }
            c.Populate(c.previewZ);
            EditorSceneManager.MarkSceneDirty(c.gameObject.scene);
            LabAutoSync.WriteStatus("OK: descubridores capturados en " + dir);
        }

        /// <summary>Las fichas que miran a la camara (FaceCamera) solo giran en Play; aqui se orientan a mano.</summary>
        static void FaceCards(Vector3 eye)
        {
            foreach (var f in Object.FindObjectsByType<FaceCamera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                var d = f.transform.position - eye;
                if (d.sqrMagnitude > 0.001f) f.transform.rotation = Quaternion.LookRotation(d, Vector3.up);
            }
        }

        static void Prepare()
        {
            foreach (var t in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                t.ForceMeshUpdate(true, true);
        }

        static void Shot(string name, Vector3 pos, Vector3 euler, float fov) => Shot(name, pos, euler, fov, 1600, 900, Dir);

        static void Shot(string name, Vector3 pos, Vector3 euler, float fov, int w, int h, string dir)
        {
            var go = new GameObject("CapturaTemporal");
            var cam = go.AddComponent<Camera>();
            cam.transform.SetPositionAndRotation(pos, Quaternion.Euler(euler));
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 100f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.gray;
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            cam.targetTexture = rt;
            cam.Render();
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            File.WriteAllBytes(Path.Combine(dir, name + ".jpg"), tex.EncodeToJPG(88));
            cam.targetTexture = null;
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(go);
        }
    }
}

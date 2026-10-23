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

        [MenuItem("Laboratorio VR/Capturar vistas", priority = 40)]
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

            if (!string.IsNullOrEmpty(current)) EditorSceneManager.OpenScene(current);
            LabAutoSync.WriteStatus("Capturas guardadas en " + Dir);
        }

        static void Prepare()
        {
            foreach (var t in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                t.ForceMeshUpdate(true, true);
        }

        static void Shot(string name, Vector3 pos, Vector3 euler, float fov)
        {
            var go = new GameObject("CapturaTemporal");
            var cam = go.AddComponent<Camera>();
            cam.transform.SetPositionAndRotation(pos, Quaternion.Euler(euler));
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 100f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.gray;
            const int w = 1600, h = 900;
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            cam.targetTexture = rt;
            cam.Render();
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            File.WriteAllBytes(Path.Combine(Dir, name + ".jpg"), tex.EncodeToJPG(88));
            cam.targetTexture = null;
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(go);
        }
    }
}

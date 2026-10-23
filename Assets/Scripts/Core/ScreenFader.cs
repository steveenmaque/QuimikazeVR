using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LabQuimicaVR
{
    /// <summary>Fundido a negro para pasar de una escena a otra (cubre los dos ojos).</summary>
    public class ScreenFader : MonoBehaviour
    {
        static ScreenFader instance;
        Image image;
        bool busy;

        /// <summary>True mientras se pasa de una escena a otra (fundido y carga).</summary>
        public static bool Busy => instance != null && instance.busy;

        static ScreenFader Get()
        {
            if (instance != null) return instance;
            var go = new GameObject("ScreenFader");
            DontDestroyOnLoad(go);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            var imgGo = new GameObject("Negro", typeof(RectTransform));
            imgGo.transform.SetParent(go.transform, false);
            var rt = (RectTransform)imgGo.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            instance = go.AddComponent<ScreenFader>();
            instance.image = imgGo.AddComponent<Image>();
            instance.image.color = new Color(0, 0, 0, 0);
            instance.image.raycastTarget = false;
            return instance;
        }

        public static void FadeToScene(string scene)
        {
            var f = Get();
            if (f.busy) return;
            f.StartCoroutine(f.Run(scene));
        }

        /// <summary>Se llama al iniciar una escena para aparecer desde negro.</summary>
        public static void FadeIn()
        {
            var f = Get();
            f.StopAllCoroutines();
            f.busy = false;
            f.StartCoroutine(f.Fade(1f, 0f, 0.6f));
        }

        IEnumerator Run(string scene)
        {
            busy = true;
            yield return Fade(0f, 1f, 0.35f);
            var op = SceneManager.LoadSceneAsync(scene);
            while (!op.isDone) yield return null;
            yield return Fade(1f, 0f, 0.5f);
            busy = false;
        }

        IEnumerator Fade(float from, float to, float time)
        {
            for (float t = 0; t < time; t += Time.unscaledDeltaTime)
            {
                image.color = new Color(0, 0, 0, Mathf.Lerp(from, to, t / time));
                yield return null;
            }
            image.color = new Color(0, 0, 0, to);
        }
    }
}

using TMPro;
using UnityEngine;

namespace LabQuimicaVR
{
    /// <summary>
    /// Transicion al entrar a un elemento: el jugador aparece rodeado por un paisaje 360 relacionado
    /// con el elemento (el espacio para el hidrogeno, una mina andina para el cobre, el mar para el sodio...)
    /// con el nombre flotando y chispas de luz. Luego el paisaje se desvanece y aparece el laboratorio.
    /// Se puede saltar con el boton del visor.
    /// </summary>
    public class ElementIntro : MonoBehaviour
    {
        /// <summary>Mientras dura la transicion no se pulsan botones del laboratorio.</summary>
        public static bool Active { get; private set; }

        const float Hold = 4.2f;
        const float FadeTime = 1.3f;

        Material sphereMat;
        TextMeshPro[] texts;
        ParticleSystem sparks;
        Transform root;
        float t;
        bool leaving;
        float leaveStart;

        public static void Play(ElementInfo e, LandscapeInfo land)
        {
            var head = GazePointer.Head;
            if (head == null || land == null) return;
            var tex = land.Load();
            if (tex == null) return;
            var go = new GameObject("TransicionPaisaje");
            var intro = go.AddComponent<ElementIntro>();
            intro.Build(head, e, land, tex);
        }

        void Build(Transform head, ElementInfo e, LandscapeInfo land, Texture2D tex)
        {
            Active = true;
            // Sigue la posicion de la cabeza pero no su giro: asi se puede mirar alrededor del paisaje.
            root = transform;
            root.position = head.position;
            float yaw = head.eulerAngles.y;
            root.rotation = Quaternion.Euler(0, yaw + 180f, 0); // el centro de la foto queda al frente

            sphereMat = new Material(LabFactory.Mat("PanoramaFade")) { mainTexture = tex, renderQueue = 3500 };
            sphereMat.SetFloat("unity_GUIZTestMode", (float)UnityEngine.Rendering.CompareFunction.Always);
            var sphere = LabFactory.MeshObject("Paisaje", MeshKit.PanoSphere(), root, Vector3.zero, Vector3.one * 9f, sphereMat);
            sphere.GetComponent<MeshRenderer>().sortingOrder = 0;

            // Titulo flotando al frente del jugador
            var front = Quaternion.Euler(0, yaw, 0);
            var titleRoot = LabFactory.Node("Titulo", null, head.position + front * new Vector3(0, 0.15f, 3.2f), front).transform;
            titleRoot.SetParent(root, true);
            var catColor = CategoryInfo.Color(e.Category);
            var t1 = LabFactory.Text(titleRoot, e.Name.ToUpper(), new Vector3(0, 0.45f, 0), 0.42f, Color.white, TextAlignmentOptions.Center, 6f, 0.7f, true);
            var t2 = LabFactory.Text(titleRoot, $"<color=#{ColorUtility.ToHtmlStringRGB(catColor)}>{e.Symbol}</color>  ·  Z = {e.Z}  ·  {CategoryInfo.Name(e.Category)}",
                new Vector3(0, -0.05f, 0), 0.17f, Color.white, TextAlignmentOptions.Center, 6f, 0.3f);
            var t3 = LabFactory.Text(titleRoot, $"<color=#FFC34D>{land.Title}</color>\n<size=70%>{ElementExtras.Sample(e).Nature}</size>",
                new Vector3(0, -0.6f, 0), 0.12f, Color.white, TextAlignmentOptions.Center, 4.6f, 0.8f);
            var t4 = LabFactory.Text(titleRoot, "Toca el botón del visor para entrar al laboratorio", new Vector3(0, -1.2f, 0), 0.07f,
                new Color(0.8f, 0.88f, 1f), TextAlignmentOptions.Center, 4f, 0.2f);
            // Sobre el paisaje (a veces un cielo claro) el halo blanco se pierde: sombra oscura y degradado al color del grupo.
            LabType.OverImage(t1);
            t1.colorGradient = new VertexGradient(Color.white, Color.white, Color.Lerp(Color.white, catColor, 0.75f), Color.Lerp(Color.white, catColor, 0.75f));
            texts = new[] { t1, t2, t3, t4 };
            foreach (var tx in texts)
            {
                tx.outlineWidth = 0.18f;
                tx.outlineColor = new Color32(0, 0, 0, 200);
                var m = tx.fontMaterial;
                m.SetFloat("unity_GUIZTestMode", (float)UnityEngine.Rendering.CompareFunction.Always);
                m.renderQueue = 3600;
            }

            // Chispas de luz alrededor
            var sp = LabFactory.Node("Chispas", root, Vector3.zero);
            sparks = sp.AddComponent<ParticleSystem>();
            var main = sparks.main;
            main.startLifetime = 3f;
            main.startSpeed = 0.15f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.08f);
            main.startColor = new ParticleSystem.MinMaxGradient(catColor, Color.white);
            main.maxParticles = 150;
            var em = sparks.emission;
            em.rateOverTime = 45f;
            var shape = sparks.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 3f;
            var r = sp.GetComponent<ParticleSystemRenderer>();
            var spark = new Material(LabFactory.Mat("Spark")) { renderQueue = 3550 };
            r.sharedMaterial = spark;
            LabAudio.Play(LabSound.Chime, 0.6f);
        }

        void Update()
        {
            t += Time.deltaTime;
            var head = GazePointer.Head;
            if (head != null) root.position = head.position;

            if (!leaving && (t > Hold || (t > 0.6f && LabInput.SelectDown)))
            {
                leaving = true;
                leaveStart = t;
                LabAudio.Play(LabSound.Whoosh, 0.7f);
                var em = sparks.emission;
                em.rateOverTime = 0f;
            }

            float a = 1f;
            if (t < 0.5f) a = t / 0.5f;
            if (leaving) a = 1f - Mathf.Clamp01((t - leaveStart) / FadeTime);
            sphereMat.color = new Color(1, 1, 1, a);
            foreach (var tx in texts) tx.alpha = a;
            if (leaving) root.localScale = Vector3.one * (1f + (t - leaveStart) * 0.6f);
            if (leaving && t - leaveStart > FadeTime)
            {
                Active = false;
                Destroy(gameObject);
            }
        }

        void OnDestroy() => Active = false;
    }
}

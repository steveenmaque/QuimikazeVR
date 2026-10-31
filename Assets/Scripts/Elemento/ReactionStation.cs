using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace LabQuimicaVR
{
    /// <summary>
    /// Laboratorio de reacciones: eliges otro elemento y en la camara de vidrio se ve como
    /// reaccionan (llamas, explosion, brillo...) y que compuesto se forma.
    /// </summary>
    public class ReactionStation : MonoBehaviour
    {
        public static readonly string[] Partners =
        {
            "H", "C", "N", "O", "F", "Na", "Mg",
            "Al", "P", "S", "Cl", "K", "Ca", "Fe",
            "Cu", "Zn", "Br", "Ag", "I", "Hg", "Xe"
        };

        ElementInfo element;
        Transform chamberCenter;
        Transform actors;
        TextMeshPro equationText, productText, descriptionText, typeText;
        readonly List<GazeButton> buttons = new List<GazeButton>();
        Coroutine running;
        Light flash;

        public void Build(ElementInfo e)
        {
            element = e;
            LabFactory.ClearChildren(transform);
            buttons.Clear();

            var size = new Vector2(3.1f, 3.0f);
            var screen = LabFactory.Screen("PantallaReacciones", transform, Vector3.zero, Quaternion.identity, size,
                "REACCIONES", Palette.Accent2);
            LabFactory.Text(screen, $"Elige un elemento para mezclarlo con {e.Article} <b>{e.Name.ToLower()}</b> ({e.Symbol})",
                new Vector3(0, 1.05f, -0.01f), 0.075f, Palette.Text, TextAlignmentOptions.Center, size.x - 0.2f, 0.2f);

            // Botones de elementos para reaccionar (7 x 3)
            const int cols = 7;
            var bsize = new Vector2(0.36f, 0.3f);
            for (int i = 0; i < Partners.Length; i++)
            {
                var p = ElementDatabase.BySymbol(Partners[i]);
                int r = i / cols, c = i % cols;
                var pos = new Vector3((c - (cols - 1) / 2f) * 0.41f, 0.72f - r * 0.35f, -0.03f);
                var b = LabFactory.Button(screen, p.Symbol, pos, bsize, 0.12f, CategoryInfo.Color(p.Category) * 0.55f, TextRole.Symbol);
                int z = p.Z;
                b.OnClick += () => Run(z);
                buttons.Add(b);
            }

            LabFactory.Box("Separador", screen, new Vector3(0, -0.28f, -0.005f), new Vector3(size.x * 0.92f, 0.01f, 0.005f), LabFactory.Tinted("GlowBase", Palette.Accent2, 1.2f));
            equationText = LabFactory.Text(screen, "", new Vector3(0, -0.45f, -0.01f), 0.13f, Color.white, TextAlignmentOptions.Center, size.x - 0.2f, 0.25f, true, TextRole.Symbol);
            productText = LabFactory.Text(screen, "Mira un botón y púlsalo para ver la reacción.", new Vector3(0, -0.66f, -0.01f), 0.085f, Palette.Accent2, TextAlignmentOptions.Center, size.x - 0.2f, 0.2f, true);
            descriptionText = LabFactory.Text(screen, "", new Vector3(0, -0.98f, -0.01f), 0.08f, Palette.Text, TextAlignmentOptions.Top, size.x - 0.3f, 0.5f);
            descriptionText.enableAutoSizing = true;
            descriptionText.fontSizeMax = 0.8f;
            descriptionText.fontSizeMin = 0.5f;
            typeText = LabFactory.Text(screen, "", new Vector3(0, -1.32f, -0.01f), 0.06f, Palette.TextDim, TextAlignmentOptions.Center, size.x - 0.2f, 0.12f);

            // Camara de reaccion delante de la pantalla
            var station = LabFactory.Node("CamaraDeReaccion", transform, new Vector3(0, 0, -1.15f)).transform;
            float floorY = -transform.position.y;
            LabFactory.Box("Mesa", station, new Vector3(0, floorY + 0.45f, 0), new Vector3(1.3f, 0.9f, 0.7f), LabFactory.Mat("BenchBody"), true);
            LabFactory.Box("Cubierta", station, new Vector3(0, floorY + 0.92f, 0), new Vector3(1.36f, 0.05f, 0.76f), LabFactory.Mat("BenchTop"));
            LabFactory.Shape("Base", PrimitiveType.Cylinder, station, new Vector3(0, floorY + 0.97f, 0), new Vector3(0.62f, 0.03f, 0.62f), LabFactory.Mat("ScreenFrame"));
            var ring = LabFactory.LoadMesh("Anillo");
            if (ring != null) LabFactory.MeshObject("Aro", ring, station, new Vector3(0, floorY + 1.0f, 0), Vector3.one * 0.32f, LabFactory.Tinted("GlowBase", Palette.Accent2, 1.5f));
            var dome = LabFactory.LoadMesh("Dome");
            if (dome != null) LabFactory.MeshObject("Cupula", dome, station, new Vector3(0, floorY + 1.0f, 0), Vector3.one * 0.3f, LabFactory.Mat("Glass"));
            chamberCenter = LabFactory.Node("Centro", station, new Vector3(0, floorY + 1.18f, 0)).transform;
            actors = LabFactory.Node("Actores", chamberCenter).transform;

            if (Application.isPlaying)
            {
                var lightGo = LabFactory.Node("Destello", chamberCenter);
                flash = lightGo.AddComponent<Light>();
                flash.type = LightType.Point;
                flash.range = 5f;
                flash.intensity = 0f;
            }
            ShowIdle();
        }

        void ShowIdle()
        {
            LabFactory.ClearChildren(actors);
            Ball(element, Vector3.zero, 0.11f);
        }

        GameObject Ball(ElementInfo e, Vector3 pos, float radius)
        {
            var ball = LabFactory.Shape("Atomo_" + e.Symbol, PrimitiveType.Sphere, actors, pos, Vector3.one * radius * 2f,
                LabFactory.Tinted("Atom", e.CpkColor, 0.15f));
            var label = LabFactory.Node("Etiqueta", ball.transform, Vector3.zero).transform;
            label.localScale = Vector3.one / (radius * 2f);
            label.gameObject.AddComponent<FaceCamera>();
            var dark = e.CpkColor.grayscale > 0.55f;
            LabFactory.Text(label, e.Symbol, new Vector3(0, 0, -radius * 1.1f), radius * 0.8f, dark ? Color.black : Color.white,
                TextAlignmentOptions.Center, radius * 3f, radius * 2f, true, TextRole.Symbol);
            return ball;
        }

        public void Run(int partnerZ)
        {
            if (running != null) StopCoroutine(running);
            running = StartCoroutine(Animate(ElementDatabase.Get(partnerZ)));
        }

        IEnumerator Animate(ElementInfo partner)
        {
            var result = ReactionDatabase.React(element, partner);
            foreach (var b in buttons) b.SetSelected(b.Label != null && b.Label.text == partner.Symbol);

            equationText.text = $"{element.Symbol} + {partner.Symbol}";
            productText.text = "Mezclando...";
            descriptionText.text = "";
            typeText.text = "";

            LabFactory.ClearChildren(actors);
            yield return null;
            var a = Ball(element, new Vector3(-0.42f, 0, 0), 0.09f);
            var b2 = Ball(partner, new Vector3(0.42f, 0, 0), 0.09f);

            bool reacts = result.Effect != ReactionEffect.None;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / 1.1f;
                float e = t * t;
                float gap = reacts ? 0.42f * (1f - e) + 0.05f * e : 0.42f - 0.3f * Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
                a.transform.localPosition = new Vector3(-gap, Mathf.Sin(t * 12f) * 0.01f, 0);
                b2.transform.localPosition = new Vector3(gap, -Mathf.Sin(t * 12f) * 0.01f, 0);
                yield return null;
            }

            yield return Effect(result);

            if (reacts)
            {
                LabFactory.ClearChildren(actors);
                yield return null;
                BuildProduct(result, partner);
            }

            equationText.text = ReactionDatabase.FormatEquation(result.Equation);
            productText.text = result.ProductName;
            descriptionText.text = result.Description;
            typeText.text = result.Predicted
                ? "Predicción usando las valencias: puede requerir calor u otras condiciones."
                : "Reacción real y conocida.";
            LabAudio.Play(LabSound.Chime, 0.6f);
            running = null;
        }

        IEnumerator Effect(ReactionResult r)
        {
            var color = r.EffectColor;
            switch (r.Effect)
            {
                case ReactionEffect.Explosion:
                    LabAudio.Play(LabSound.Boom);
                    Burst(color, 260, 2.8f, 0.07f, 0.8f, 0f);
                    yield return Flash(color, 9f, 0.6f);
                    break;
                case ReactionEffect.Fire:
                    LabAudio.Play(LabSound.Fizz);
                    Flames(color, 1.6f);
                    yield return Flash(color, 4f, 1.6f);
                    break;
                case ReactionEffect.BrightLight:
                    LabAudio.Play(LabSound.Fizz);
                    Burst(Color.white, 140, 1.2f, 0.05f, 1.4f, 0f);
                    yield return Flash(Color.white, 12f, 1.6f);
                    break;
                case ReactionEffect.Smoke:
                    LabAudio.Play(LabSound.Fizz, 0.6f);
                    Smoke(color);
                    yield return Flash(color, 2f, 1.2f);
                    break;
                case ReactionEffect.Glow:
                    Burst(color, 60, 0.6f, 0.04f, 1.2f, 0f);
                    yield return Flash(color, 5f, 1.4f);
                    break;
                case ReactionEffect.Slow:
                    Burst(color, 40, 0.25f, 0.035f, 1.8f, -0.05f);
                    yield return new WaitForSeconds(1.2f);
                    break;
                case ReactionEffect.Alloy:
                    Burst(color, 90, 0.4f, 0.04f, 1.5f, 0f);
                    yield return Flash(color, 3f, 1.2f);
                    break;
                default:
                    yield return new WaitForSeconds(0.6f);
                    break;
            }
        }

        IEnumerator Flash(Color color, float intensity, float duration)
        {
            if (flash == null) { yield return new WaitForSeconds(duration); yield break; }
            flash.color = color;
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                float k = t / duration;
                flash.intensity = intensity * (k < 0.1f ? k / 0.1f : 1f - (k - 0.1f) / 0.9f);
                yield return null;
            }
            flash.intensity = 0f;
        }

        ParticleSystem NewSystem(string name, int max)
        {
            var go = LabFactory.Node(name, chamberCenter);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = false;
            main.duration = 2f;
            main.maxParticles = max;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = LabFactory.Mat("Spark");
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.05f;
            Destroy(go, 5f);
            return ps;
        }

        static Gradient Fade(Color c)
        {
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.Lerp(c, Color.white, 0.5f), 0f), new GradientColorKey(c, 0.4f), new GradientColorKey(c * 0.6f, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0f, 1f) });
            return g;
        }

        void Burst(Color color, int count, float speed, float size, float life, float gravity)
        {
            var ps = NewSystem("Chispas", count + 10);
            var main = ps.main;
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.3f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
            main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.5f, life);
            main.startColor = color;
            main.gravityModifier = gravity;
            var em = ps.emission;
            em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
            var col = ps.colorOverLifetime;
            col.enabled = true;
            col.color = Fade(color);
            var drag = ps.limitVelocityOverLifetime;
            drag.enabled = true;
            drag.drag = 2.5f;
            ps.Play();
        }

        void Flames(Color color, float duration)
        {
            var ps = NewSystem("Llamas", 400);
            var main = ps.main;
            main.duration = duration;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.45f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
            main.startColor = color;
            main.gravityModifier = -0.25f;
            var em = ps.emission;
            em.rateOverTime = 180f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.08f;
            shape.rotation = new Vector3(-90f, 0f, 0f);
            var col = ps.colorOverLifetime;
            col.enabled = true;
            col.color = Fade(color);
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0.1f));
            ps.Play();
        }

        void Smoke(Color color)
        {
            var ps = NewSystem("Humo", 120);
            var main = ps.main;
            main.duration = 1.4f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.18f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.25f);
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.2f);
            main.startColor = Color.Lerp(color, Color.gray, 0.4f);
            main.gravityModifier = -0.05f;
            var em = ps.emission;
            em.rateOverTime = 60f;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            col.color = Fade(Color.Lerp(color, Color.gray, 0.4f));
            ps.Play();
        }

        void BuildProduct(ReactionResult r, ElementInfo partner)
        {
            var atoms = ReactionDatabase.ParseFormula(r.Formula);
            var holder = LabFactory.Node("Producto", actors).transform;
            holder.gameObject.AddComponent<Spin>().degreesPerSecond = new Vector3(0, 35, 0);

            if (atoms.Count == 0)
            {
                // Mezcla / aleacion: una sola esfera con el color combinado
                var mixed = LabFactory.Shape("Aleacion", PrimitiveType.Sphere, holder, Vector3.zero, Vector3.one * 0.22f,
                    LabFactory.Tinted("Sample", r.EffectColor));
                var m = mixed.GetComponent<Renderer>().sharedMaterial;
                if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0.9f);
                if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.85f);
                return;
            }

            var list = new List<ElementInfo>();
            foreach (var (symbol, count) in atoms)
            {
                var el = ElementDatabase.BySymbol(symbol);
                if (el == null) continue;
                for (int i = 0; i < count && list.Count < 14; i++) list.Add(el);
            }
            if (list.Count == 0) return;

            // El atomo central es el que aparece una sola vez (si existe).
            int centerIndex = list.FindIndex(x => list.FindAll(y => y == x).Count == 1);
            if (centerIndex < 0) centerIndex = 0;
            var center = list[centerIndex];
            list.RemoveAt(centerIndex);

            float rc = 0.075f;
            BallIn(holder, center, Vector3.zero, rc);
            float golden = Mathf.PI * (3f - Mathf.Sqrt(5f));
            int n = list.Count;
            var bondMat = LabFactory.Mat("Bond");
            for (int i = 0; i < n; i++)
            {
                Vector3 dir;
                if (n == 1) dir = Vector3.right;
                else if (n == 2) dir = Quaternion.Euler(0, 0, i == 0 ? 52f : -52f) * Vector3.down;
                else
                {
                    float y = 1f - 2f * (i + 0.5f) / n;
                    float ring = Mathf.Sqrt(1f - y * y);
                    dir = new Vector3(Mathf.Cos(golden * i) * ring, y, Mathf.Sin(golden * i) * ring);
                }
                float dist = 0.19f + (n > 6 ? 0.06f : 0f);
                BallIn(holder, list[i], dir * dist, 0.06f);
                var bond = LabFactory.Shape("Enlace", PrimitiveType.Cylinder, holder, dir * dist * 0.5f, new Vector3(0.025f, dist * 0.5f, 0.025f), bondMat);
                bond.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir);
            }
        }

        void BallIn(Transform parent, ElementInfo e, Vector3 pos, float radius)
        {
            var ball = LabFactory.Shape("Atomo_" + e.Symbol, PrimitiveType.Sphere, parent, pos, Vector3.one * radius * 2f,
                LabFactory.Tinted("Atom", e.CpkColor, 0.15f));
            var label = LabFactory.Node("Etiqueta", ball.transform).transform;
            label.localScale = Vector3.one / (radius * 2f);
            label.gameObject.AddComponent<FaceCamera>();
            var dark = e.CpkColor.grayscale > 0.55f;
            LabFactory.Text(label, e.Symbol, new Vector3(0, 0, -radius * 1.1f), radius * 0.75f, dark ? Color.black : Color.white, TextAlignmentOptions.Center, radius * 3f, radius * 2f, true, TextRole.Symbol);
        }
    }
}

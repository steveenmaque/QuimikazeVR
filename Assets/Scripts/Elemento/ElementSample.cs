using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace LabQuimicaVR
{
    /// <summary>
    /// Vitrina con la muestra real del elemento: un modelo 3D hecho a semejanza de las fotos reales
    /// (forma, color, brillo metalico, recipiente) y, encima, la foto real con su descripcion.
    /// Los datos de cada elemento estan en Resources/Datos/muestras.txt.
    /// </summary>
    public static class ElementSample
    {
        static readonly Color[] Rainbow =
        {
            new Color(1f, 0.82f, 0.3f), new Color(0.88f, 0.3f, 0.69f), new Color(0.24f, 0.48f, 1f),
            new Color(0.24f, 0.86f, 0.6f), new Color(0.75f, 0.55f, 1f)
        };

        public static void Build(Transform anchor, ElementInfo e)
        {
            var s = ElementExtras.Sample(e);

            // Vitrina: pedestal con tapa de terciopelo oscuro para que resalte el brillo de la muestra.
            LabFactory.Shape("Pedestal", PrimitiveType.Cylinder, anchor, new Vector3(0, 0.45f, 0), new Vector3(0.96f, 0.45f, 0.96f), LabFactory.Mat("Pedestal"), true);
            LabFactory.Shape("Terciopelo", PrimitiveType.Cylinder, anchor, new Vector3(0, 0.905f, 0), new Vector3(0.92f, 0.008f, 0.92f), LabFactory.Tinted("Card", new Color(0.4f, 0.42f, 0.46f)));
            LabFactory.Shape("Aro", PrimitiveType.Cylinder, anchor, new Vector3(0, 0.9f, 0), new Vector3(1.0f, 0.012f, 1.0f), LabFactory.Tinted("GlowBase", Palette.Accent, 1.4f));

            var holder = LabFactory.Node("Muestra", anchor, new Vector3(0, 0.92f, 0)).transform;
            holder.localScale = Vector3.one * 1.9f;
            var spin = holder.gameObject.AddComponent<Spin>();
            spin.degreesPerSecond = new Vector3(0, 16, 0);

            int seed = e.Z * 7919;
            switch (s.Form)
            {
                case "tubo": Tube(holder, s); break;
                case "liquido_frio": ColdLiquid(holder, s); break;
                case "gas": GasFlask(holder, s); break;
                case "cubo_acrilico": AcrylicCube(holder, s, e); break;
                case "ampolla": Ampoule(holder, s, seed); break;
                case "cubo": CubeSet(holder, s, seed); break;
                case "barra": RodSet(holder, s, seed); break;
                case "cristales": Crystals(holder, s, seed); break;
                case "trozos": Chunks(holder, s, seed); break;
                case "escamas": Flakes(holder, s, seed); break;
                case "nodulos": Nodules(holder, s, seed); break;
                case "pepita": Nugget(holder, s, seed); break;
                case "bismuto": Bismuth(holder, s); break;
                case "polvo": Powder(holder, s, seed); break;
                case "cristal_transparente": YellowCrystals(holder, s, seed); break;
                case "carbono": Carbon(holder, s, seed); break;
                case "yodo": Iodine(holder, s, seed); break;
                case "charco": Mercury(holder, s, seed); break;
                case "brillo": Glowing(holder, s, seed); break;
                default: Unseen(holder, s); break;
            }

            BuildInfoCard(anchor, e, s);
        }

        // =================================================================== materiales
        static Material Metal(Color c, float metallic, float smooth)
        {
            var m = new Material(LabFactory.Mat("Sample")) { name = "Muestra_" + ColorUtility.ToHtmlStringRGB(c) };
            // Los metales reflejan la sala blanca: se oscurecen un poco para que se vean como en las fotos.
            m.color = metallic > 0.5f ? c * 0.85f : c;
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smooth);
            m.DisableKeyword("_EMISSION");
            return m;
        }

        static Material Main(SampleInfo s) => Metal(s.Color, s.Metallic, s.Smoothness);

        /// <summary>Capa de oxido o costra: mas oscura y opaca que el metal.</summary>
        static Material Crust(SampleInfo s) =>
            s.Color2.HasValue ? Metal(s.Color2.Value, 0.15f, 0.2f)
                              : Metal(s.Color * 0.7f, s.Metallic * 0.4f, 0.22f);

        /// <summary>Material que brilla con su propio color (el color sale de la emision, no de la luz).</summary>
        static Material Glow(Color c, float intensity)
        {
            var m = new Material(LabFactory.Mat("GlowBase")) { name = "Brillo_" + ColorUtility.ToHtmlStringRGB(c) };
            m.color = c * 0.15f;
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c * intensity);
            return m;
        }

        static Material Glass => LabFactory.Mat("Glass");

        /// <summary>Vidrio delgado y casi invisible (tubos de gas y ampollas): deja ver bien lo de adentro.</summary>
        static Material ThinGlass => LabFactory.Tinted("Glass", new Color(0.9f, 0.95f, 1f, 0.07f), 0f);

        /// <summary>Liquido de color que deja pasar la luz (bromo, cloro): brilla un poco con su propio color.</summary>
        static Material Translucent(Color c)
        {
            var m = Metal(c, 0f, 0.92f);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c * 0.45f);
            return m;
        }

        static Material Transparent(Color c, float alpha, float emission = 0f) =>
            LabFactory.Tinted("GasGlow", new Color(c.r, c.g, c.b, alpha), emission);

        static GameObject Piece(string name, Mesh mesh, Transform parent, Vector3 pos, Quaternion rot, float scale, Material mat)
        {
            var go = LabFactory.MeshObject(name, mesh, parent, pos, Vector3.one * scale, mat);
            go.transform.localRotation = rot;
            return go;
        }

        static GameObject Piece(string name, Mesh mesh, Transform parent, Vector3 pos, Quaternion rot, float scale, Material[] mats)
        {
            var go = Piece(name, mesh, parent, pos, rot, scale, mats[0]);
            go.GetComponent<MeshRenderer>().sharedMaterials = mats;
            return go;
        }

        static void AddLight(Transform parent, Vector3 pos, Color c, float intensity, float range)
        {
            var l = LabFactory.Node("Luz", parent, pos).AddComponent<Light>();
            l.type = LightType.Point;
            l.color = c;
            l.intensity = intensity;
            l.range = range;
            l.shadows = LightShadows.None;
            l.renderMode = LightRenderMode.ForcePixel;
        }

        static float Size(SampleInfo s) => s.Has("pequeno") ? 0.55f : 1f;

        // =================================================================== formas
        static void Tube(Transform h, SampleInfo s)
        {
            // Tubo de descarga acostado sobre dos soportes, como en las fotos: el gas brilla con su color.
            float len = 0.36f, r = 0.022f;
            var stand = LabFactory.Mat("Metal");
            foreach (var x in new[] { -0.13f, 0.13f })
            {
                LabFactory.Shape("Soporte", PrimitiveType.Cylinder, h, new Vector3(x, 0.035f, 0), new Vector3(0.012f, 0.035f, 0.012f), stand);
                LabFactory.Shape("Pinza", PrimitiveType.Cylinder, h, new Vector3(x, 0.07f, 0), new Vector3(0.05f, 0.006f, 0.05f), stand);
            }
            var tube = LabFactory.Node("Tubo", h, new Vector3(len / 2, 0.095f, 0), Quaternion.Euler(0, 0, 90)).transform;
            LabFactory.MeshObject("Vidrio", MeshKit.Capsule(len, r), tube, Vector3.zero, Vector3.one, ThinGlass);
            // Nucleo brillante, halo de color y los extremos (cerca de los electrodos el color cambia)
            LabFactory.MeshObject("Brillo", MeshKit.Capsule(len - 0.07f, r * 0.42f), tube, new Vector3(0, 0.035f, 0), Vector3.one, Glow(s.Color, 1.05f));
            LabFactory.MeshObject("Halo", MeshKit.Capsule(len - 0.03f, r * 0.85f), tube, new Vector3(0, 0.015f, 0), Vector3.one, Transparent(s.Color, 0.3f, 0.6f));
            var endColor = s.Color2 ?? s.Color;
            foreach (var y in new[] { 0.03f, len - 0.03f })
            {
                LabFactory.Shape("Electrodo", PrimitiveType.Cylinder, tube, new Vector3(0, y, 0), new Vector3(0.026f, 0.014f, 0.026f), LabFactory.Mat("Metal"));
                LabFactory.Shape("Punta", PrimitiveType.Sphere, tube, new Vector3(0, y + (y < 0.1f ? 0.03f : -0.03f), 0), new Vector3(0.022f, 0.04f, 0.022f), Glow(endColor, 1.2f));
            }
            AddLight(h, new Vector3(0, 0.16f, -0.05f), s.Color, 1.8f, 1.4f);
        }

        static void ColdLiquid(Transform h, SampleInfo s)
        {
            var beaker = LabFactory.LoadMesh("Vaso");
            var liquidMesh = LabFactory.LoadMesh("LiquidoVaso");
            LabFactory.MeshObject("Vaso", beaker, h, Vector3.zero, Vector3.one * 2.6f, Glass);
            // El nitrogeno liquido es transparente; el oxigeno liquido, celeste.
            float alpha = s.Color.grayscale > 0.9f ? 0.18f : 0.6f;
            LabFactory.MeshObject("Liquido", liquidMesh, h, Vector3.zero, Vector3.one * 2.6f, Transparent(s.Color, alpha, 0.1f));
            // Niebla fria que sale y cae por los lados
            var fog = LabFactory.Node("Niebla", h, new Vector3(0, 0.2f, 0));
            fog.transform.localRotation = Quaternion.Euler(-90, 0, 0);
            var ps = fog.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 2.6f;
            main.startSpeed = 0.06f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
            main.startColor = new Color(1f, 1f, 1f, 0.35f);
            main.gravityModifier = 0.02f;
            main.maxParticles = 60;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission;
            em.rateOverTime = 18f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.09f;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(0.45f, 0.2f), new GradientAlphaKey(0, 1) });
            col.color = g;
            var sizeLife = ps.sizeOverLifetime;
            sizeLife.enabled = true;
            sizeLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 0.6f, 1, 1.8f));
            fog.GetComponent<ParticleSystemRenderer>().sharedMaterial = LabFactory.Mat("CloudParticle");
        }

        static void GasFlask(Transform h, SampleInfo s)
        {
            var flask = LabFactory.LoadMesh("Balon");
            LabFactory.MeshObject("Balon", flask, h, Vector3.zero, Vector3.one * 2.4f, Glass);
            LabFactory.Shape("Gas", PrimitiveType.Sphere, h, new Vector3(0, 0.11f, 0), Vector3.one * 0.19f, Transparent(s.Color, s.Color.grayscale > 0.9f ? 0.12f : 0.45f, 0.25f));
            LabFactory.Shape("Tapon", PrimitiveType.Cylinder, h, new Vector3(0, 0.33f, 0), new Vector3(0.05f, 0.02f, 0.05f), LabFactory.Mat("BenchTop"));
        }

        static void AcrylicCube(Transform h, SampleInfo s, ElementInfo e)
        {
            float size = 0.24f;
            var cube = LabFactory.MeshObject("Acrilico", MeshKit.BeveledBox(Vector3.one * size, 0.012f), h, new Vector3(0, size / 2, 0), Vector3.one, Glass);
            var amp = LabFactory.Node("Ampolla", h, new Vector3(0, size * 0.18f, 0), Quaternion.Euler(0, 0, -18)).transform;
            LabFactory.MeshObject("Vidrio", MeshKit.Ampoule(0.022f, 0.16f), amp, Vector3.zero, Vector3.one, Glass);
            bool liquid = s.Has("liquido");
            if (liquid)
            {
                LabFactory.MeshObject("Liquido", MeshKit.Capsule(0.1f, 0.018f), amp, new Vector3(0, 0.004f, 0), Vector3.one, Translucent(s.Color));
                LabFactory.MeshObject("Vapor", MeshKit.Capsule(0.04f, 0.017f), amp, new Vector3(0, 0.1f, 0), Vector3.one, Transparent(Color.Lerp(s.Color, new Color(1f, 0.45f, 0.1f), 0.6f), 0.5f));
            }
            else
            {
                LabFactory.MeshObject("Liquido", MeshKit.Capsule(0.035f, 0.018f), amp, new Vector3(0, 0.004f, 0), Vector3.one, Translucent(s.Color));
                LabFactory.MeshObject("Gas", MeshKit.Capsule(0.1f, 0.017f), amp, new Vector3(0, 0.04f, 0), Vector3.one, Transparent(s.Color, 0.4f, 0.2f));
            }
            // Etiqueta negra con el numero y el simbolo, como en las colecciones de elementos
            var label = LabFactory.Node("Etiqueta", h, new Vector3(-size * 0.22f, size * 0.12f, -size / 2 - 0.002f)).transform;
            LabFactory.Box("Fondo", label, Vector3.zero, new Vector3(0.09f, 0.04f, 0.002f), LabFactory.Mat("Screen"));
            LabFactory.Text(label, $"<b>{e.Z}</b> {e.Symbol}", new Vector3(0, 0, -0.003f), 0.022f, Color.white, TextAlignmentOptions.Center, 0.09f, 0.04f, true, TextRole.Symbol);
        }

        static void Ampoule(Transform h, SampleInfo s, int seed)
        {
            // Ampolla recostada sobre dos soportes
            var amp = LabFactory.Node("Ampolla", h, new Vector3(0, 0.07f, 0), Quaternion.Euler(0, 0, 90)).transform;
            float len = 0.36f, r = 0.066f;
            amp.localPosition = new Vector3(len / 2, 0.07f, 0);
            LabFactory.MeshObject("Vidrio", MeshKit.Ampoule(r, len), amp, Vector3.zero, Vector3.one, ThinGlass);
            foreach (var x in new[] { -0.1f, 0.1f })
                LabFactory.Box("Soporte", h, new Vector3(x, 0.006f, 0), new Vector3(0.03f, 0.012f, 0.1f), LabFactory.Mat("BenchTop"));

            var mat = Main(s);
            // Contenido (coordenadas de la ampolla: Y a lo largo, el fondo esta en y=0)
            if (s.Has("liquido"))
            {
                var liquid = LabFactory.MeshObject("Liquido", MeshKit.Capsule(0.17f, r * 0.85f), amp, new Vector3(-r * 0.25f, 0.01f, 0), new Vector3(0.62f, 1f, 1f), mat);
                liquid.transform.localScale = new Vector3(0.55f, 1f, 1f);
            }
            else if (s.Has("lamina"))
                Piece("Lamina", MeshKit.Flake(seed, 0.05f, 0.01f), amp, new Vector3(-r * 0.4f, 0.12f, 0), Quaternion.Euler(0, 0, 90), 1f, mat);
            else if (s.Has("cubo"))
                Piece("Cubo", MeshKit.BeveledBox(Vector3.one * 0.07f, 0.005f), amp, new Vector3(-r * 0.42f, 0.12f, 0), Quaternion.Euler(0, 20, 0), 1f, mat);
            else if (s.Has("cristales"))
                Piece("Cristales", MeshKit.CrystalCluster(seed, 10, 0.12f, true, 4), amp, new Vector3(-r * 0.55f, 0.04f, 0), Quaternion.Euler(0, 0, -90), 1f, mat);
            else
            {
                var rnd = new System.Random(seed);
                for (int i = 0; i < 3; i++)
                {
                    float sz = 0.042f + (float)rnd.NextDouble() * 0.014f;
                    var mesh = MeshKit.Rock(seed + i, new Vector3(sz * 1.4f, sz, sz), 0.3f, 1, true, 0.4f, i == 0);
                    Piece("Trozo", mesh, amp, new Vector3(-r * 0.45f, 0.06f + i * 0.055f, ((float)rnd.NextDouble() - 0.5f) * 0.03f),
                        Quaternion.Euler((float)rnd.NextDouble() * 360, (float)rnd.NextDouble() * 360, 0), 1f, mat);
                }
            }
        }

        static void CubeSet(Transform h, SampleInfo s, int seed)
        {
            var mat = Main(s);
            Piece("Cubo", MeshKit.BeveledBox(Vector3.one * 0.11f, 0.006f), h, new Vector3(-0.08f, 0.055f, 0.02f), Quaternion.Euler(0, 25, 0), 1f, mat);
            if (s.Has("lamina"))
            {
                // Lamina o pieza pulida y delgada
                var slab = MeshKit.BeveledBox(new Vector3(0.2f, 0.012f, 0.15f), 0.003f);
                Piece("Lamina", slab, h, new Vector3(0.1f, 0.01f, -0.02f), Quaternion.Euler(0, -15, 0), 1f, mat);
                Piece("Lamina2", slab, h, new Vector3(0.08f, 0.03f, 0.05f), Quaternion.Euler(4, 20, -6), 0.8f, mat);
            }
            else
                Chunks(h, s, seed, new Vector3(0.1f, 0, -0.02f), 0.75f);
        }

        static void RodSet(Transform h, SampleInfo s, int seed)
        {
            bool crystal = s.Has("cristalina");
            var mat = Main(s);
            if (s.Has("varias"))
            {
                // Varias barras fundidas una al lado de otra (como las barras de estano)
                for (int i = 0; i < 3; i++)
                    Piece("Barra", MeshKit.Rod(seed + i, 0.3f - i * 0.04f, 0.026f, false), h, new Vector3(0.02f * i, 0.03f, -0.09f + i * 0.08f),
                        Quaternion.Euler(0, 8 * i - 8, 90), 1f, mat);
                return;
            }
            Piece("Barra", MeshKit.Rod(seed, 0.36f, 0.036f, crystal), h, new Vector3(0, 0.045f, 0), Quaternion.Euler(0, 0, 90), 1f, mat);
            if (s.Has("iridiscente"))
            {
                // Extremo con capa de oxido de colores (como la barra de wolframio)
                for (int i = 0; i < 4; i++)
                    Piece("Oxido", MeshKit.Rock(seed + i * 3, Vector3.one * 0.026f, 0.25f, 0, true, 0.6f), h,
                        new Vector3(-0.12f - i * 0.018f, 0.06f + (i % 2) * 0.02f, (i % 3 - 1) * 0.02f), Quaternion.Euler(i * 40, i * 70, 0), 1f,
                        Metal(Rainbow[i % Rainbow.Length], 0.9f, 0.85f));
            }
            if (s.Has("cubo"))
                Piece("Cubo", MeshKit.BeveledBox(Vector3.one * 0.09f, 0.005f), h, new Vector3(0.08f, 0.045f, 0.13f), Quaternion.Euler(0, 30, 0), 1f, mat);
            if (crystal)
                Piece("Barra2", MeshKit.Rod(seed + 5, 0.28f, 0.03f, true), h, new Vector3(0.02f, 0.04f, -0.1f), Quaternion.Euler(0, 15, 90), 1f, mat);
        }

        static void Crystals(Transform h, SampleInfo s, int seed)
        {
            var mat = Main(s);
            bool branches = s.Has("ramas");
            int sides = branches ? 4 : 6;
            if (branches)
            {
                // Dendritas (cobre nativo, plata, oro): ramas finas de cristalitos
                Piece("Dendrita", MeshKit.Dendrite(seed, 0.6f, 6), h, new Vector3(0, 0.01f, 0), Quaternion.identity, 1f, mat);
            }
            else if (s.Has("prismas"))
                Piece("Cristales", MeshKit.CrystalCluster(seed, 16, 0.34f, false, sides), h, new Vector3(0, 0.03f, 0), Quaternion.identity, 1f, mat);
            else
            {
                // Masa de cristalitos apretados, como en las fotos de metales puros
                float grain = s.Has("denso") ? 0.016f : 0.022f;
                int count = s.Has("denso") ? 260 : 170;
                Piece("Masa", MeshKit.CrystalMass(seed, new Vector3(0.3f, 0.11f, 0.2f), count, grain), h, new Vector3(-0.03f, 0.055f, 0), Quaternion.Euler(0, 15, 0), 1f, mat);
                Piece("Masa2", MeshKit.CrystalMass(seed + 3, new Vector3(0.14f, 0.07f, 0.11f), count / 3, grain), h, new Vector3(0.1f, 0.035f, -0.12f), Quaternion.Euler(0, 70, 0), 1f, mat);
            }
            if (s.Has("cubo"))
            {
                var cubeMat = s.Has("anodizado") && s.Color2.HasValue ? Metal(s.Color2.Value, 0.85f, 0.7f) : mat;
                Piece("Cubo", MeshKit.BeveledBox(Vector3.one * 0.09f, 0.005f), h, new Vector3(0.17f, 0.045f, 0.1f), Quaternion.Euler(0, 30, 0), 1f, cubeMat);
            }
        }

        static void Chunks(Transform h, SampleInfo s, int seed) => Chunks(h, s, seed, Vector3.zero, Size(s));

        static void Chunks(Transform h, SampleInfo s, int seed, Vector3 offset, float scale)
        {
            if (s.Has("anillo"))
            {
                // Plutonio: anillo metalico cubierto de oxido oscuro (como en la foto)
                var ring = MeshKit.Lathe(new[] { new Vector2(0.06f, 0f), new Vector2(0.1f, 0f), new Vector2(0.1f, 0.07f), new Vector2(0.06f, 0.07f), new Vector2(0.06f, 0f) }, 36, "Anillo");
                Piece("Anillo", ring, h, offset, Quaternion.identity, scale, Main(s));
                return;
            }
            bool smooth = s.Has("lisa");
            bool cut = s.Has("cara_cortada");
            float rough = s.Has("rugoso") ? 0.45f : s.Has("facetas") ? 0.3f : s.Has("laminar") ? 0.38f : smooth ? 0.12f : 0.36f;
            int subdiv = s.Has("facetas") ? 1 : 3;
            var rnd = new System.Random(seed);
            int count = scale < 0.7f ? 1 : 3;
            for (int i = 0; i < count; i++)
            {
                float sz = (i == 0 ? 0.13f : 0.075f + (float)rnd.NextDouble() * 0.03f) * scale;
                var meshScale = new Vector3(sz * 1.3f, sz * (s.Has("laminar") ? 0.6f : 0.85f), sz);
                var mesh = MeshKit.Rock(seed + i * 13, meshScale, rough, i == 0 ? subdiv : Mathf.Min(subdiv, 2), !smooth, s.Has("facetas") ? 0.7f : 0.3f, cut && i == 0);
                var pos = offset + (i == 0 ? new Vector3(0, sz * 0.75f, 0) : new Vector3((i == 1 ? -1 : 1) * 0.15f * scale, sz * 0.6f, ((float)rnd.NextDouble() - 0.3f) * 0.1f));
                var rot = i == 0 ? Quaternion.Euler(0, 20, 0) : Quaternion.Euler((float)rnd.NextDouble() * 50, (float)rnd.NextDouble() * 360, (float)rnd.NextDouble() * 40);
                if (cut && i == 0)
                {
                    // La cara cortada brilla (metal fresco) y el resto tiene la costra de oxido.
                    SplitCutFace(mesh, new Vector3(0.3f, 1f, 0.2f).normalized);
                    Piece("Trozo", mesh, h, pos, rot, 1f, new[] { Crust(s), Main(s) });
                }
                else
                    Piece("Trozo", mesh, h, pos, rot, 1f, cut ? Crust(s) : Main(s));
            }
        }

        /// <summary>Separa en una segunda submalla los triangulos de la cara plana del corte.</summary>
        static void SplitCutFace(Mesh m, Vector3 cutNormal)
        {
            var ts = m.triangles; var ns = m.normals;
            var crust = new List<int>(); var cut = new List<int>();
            for (int i = 0; i < ts.Length; i += 3)
                (Vector3.Dot(ns[ts[i]], cutNormal) > 0.97f ? cut : crust).AddRange(new[] { ts[i], ts[i + 1], ts[i + 2] });
            m.subMeshCount = 2;
            m.SetTriangles(crust, 0);
            m.SetTriangles(cut, 1);
        }

        static void Flakes(Transform h, SampleInfo s, int seed)
        {
            var mat = Main(s);
            var tint = s.Has("iridiscente") && s.Color2.HasValue ? Metal(s.Color2.Value, 0.9f, 0.6f) : null;
            var rnd = new System.Random(seed);
            for (int i = 0; i < 6; i++)
            {
                float a = i * 60f + (float)rnd.NextDouble() * 30f;
                var pos = Quaternion.Euler(0, a, 0) * new Vector3(0.11f + (float)rnd.NextDouble() * 0.05f, 0.012f + (i % 2) * 0.012f, 0);
                var m = (tint != null && i % 2 == 0) ? tint : mat;
                float fs = 0.13f + (float)rnd.NextDouble() * 0.05f;
                Piece("Escama", MeshKit.CrystalMass(seed + i, new Vector3(fs, 0.022f, fs * 0.8f), 40, 0.011f), h, pos,
                    Quaternion.Euler(((float)rnd.NextDouble() - 0.5f) * 25, a, ((float)rnd.NextDouble() - 0.5f) * 25), 1f, m);
            }
            // Las fotos de hierro, cobalto y manganeso muestran las escamas junto a un cubo pulido de 1 cm.
            Piece("Cubo", MeshKit.BeveledBox(Vector3.one * 0.09f, 0.005f), h, new Vector3(0, 0.045f, 0), Quaternion.Euler(0, 15, 0), 1f, Metal(s.Color * 1.05f, s.Metallic, 0.9f));
        }

        static void Nodules(Transform h, SampleInfo s, int seed)
        {
            var mat = s.Has("brillo") && s.Color2.HasValue ? GlowMetal(s) : Main(s);
            var rnd = new System.Random(seed);
            if (s.Has("pequeno"))
            {
                Piece("Esfera", MeshKit.Blob(seed, Vector3.one * 0.07f, 0.04f), h, new Vector3(0, 0.07f, 0), Quaternion.identity, 1f, mat);
                LabFactory.Shape("Base", PrimitiveType.Cylinder, h, new Vector3(0, 0.01f, 0), new Vector3(0.12f, 0.01f, 0.12f), LabFactory.Mat("Metal"));
                return;
            }
            bool beads = s.Z == 4 || s.Z == 61; // esferas sueltas
            if (beads)
            {
                for (int i = 0; i < 4; i++)
                {
                    float sz = i == 0 ? 0.085f : 0.045f + (float)rnd.NextDouble() * 0.015f;
                    var pos = i == 0 ? new Vector3(0, sz * 0.8f, 0) : Quaternion.Euler(0, i * 120, 0) * new Vector3(0.14f, sz * 0.8f, 0);
                    Piece("Bolita", MeshKit.Blob(seed + i, new Vector3(sz, sz * 0.85f, sz), 0.1f), h, pos, Quaternion.identity, 1f, mat);
                }
            }
            else
            {
                // Racimo botrioidal: muchas bolitas fundidas
                for (int i = 0; i < 22; i++)
                {
                    float sz = 0.03f + (float)rnd.NextDouble() * 0.03f;
                    var pos = new Vector3(((float)rnd.NextDouble() - 0.5f) * 0.2f, sz * 0.6f + (float)rnd.NextDouble() * 0.06f, ((float)rnd.NextDouble() - 0.5f) * 0.16f);
                    Piece("Nodulo", MeshKit.Blob(seed + i, Vector3.one * sz, 0.15f), h, pos, Quaternion.identity, 1f, mat);
                }
            }
            if (s.Has("cubo"))
                Piece("Cubo", MeshKit.BeveledBox(Vector3.one * 0.09f, 0.005f), h, new Vector3(0.18f, 0.045f, 0.08f), Quaternion.Euler(0, 30, 0), 1f, Metal(s.Color, s.Metallic, 0.8f));
            if (s.Has("brillo")) AddLight(h, new Vector3(0, 0.15f, 0), s.Color2 ?? Color.white, 0.5f, 0.8f);
        }

        static Material GlowMetal(SampleInfo s)
        {
            var m = Main(s);
            if (m.HasProperty("_EmissionColor"))
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", (s.Color2 ?? Color.white) * (s.Form == "brillo" ? 0.6f : 0.22f));
            }
            return m;
        }

        static void Nugget(Transform h, SampleInfo s, int seed)
        {
            var mat = Main(s);
            // Como en la foto: cristales de oro en ramas, y al lado pepitas como las que se hallan en los rios.
            if (s.Has("ramas"))
                Piece("Cristales", MeshKit.Dendrite(seed + 3, 0.5f, 5), h, new Vector3(-0.04f, 0.01f, 0.02f), Quaternion.Euler(0, 60, 0), 1f, mat);
            Piece("Pepita", MeshKit.Rock(seed, new Vector3(0.06f, 0.035f, 0.05f), 0.55f, 3, true, 0.15f), h, new Vector3(0.13f, 0.03f, -0.08f), Quaternion.Euler(0, 30, 0), 1f, mat);
            Piece("Pepita2", MeshKit.Rock(seed + 1, new Vector3(0.035f, 0.022f, 0.03f), 0.55f, 2, true, 0.15f), h, new Vector3(0.17f, 0.02f, 0.05f), Quaternion.Euler(0, 80, 0), 1f, mat);
            Piece("Pepita3", MeshKit.Rock(seed + 2, new Vector3(0.025f, 0.018f, 0.022f), 0.6f, 2, true, 0.15f), h, new Vector3(0.06f, 0.016f, -0.15f), Quaternion.identity, 1f, mat);
        }

        static void Bismuth(Transform h, SampleInfo s)
        {
            var steps = MeshKit.Hopper(5, 0.2f);
            for (int k = 0; k < 2; k++)
            {
                var root = LabFactory.Node("Tolva", h, k == 0 ? new Vector3(-0.04f, 0, 0) : new Vector3(0.13f, 0.0f, 0.08f),
                    Quaternion.Euler(k == 0 ? 0 : 12, k == 0 ? 20 : -35, 0)).transform;
                root.localScale = Vector3.one * (k == 0 ? 1f : 0.6f);
                for (int i = 0; i < steps.Length; i++)
                {
                    // Colores del oxido: cada escalon tiene un tono distinto
                    var c = Color.Lerp(s.Color, Rainbow[(i + k * 2) % Rainbow.Length], 0.8f);
                    LabFactory.MeshObject("Escalon", steps[i], root, Vector3.zero, Vector3.one, Metal(c, 1f, 0.85f));
                }
            }
        }

        static void Powder(Transform h, SampleInfo s, int seed)
        {
            LabFactory.MeshObject("VidrioReloj", MeshKit.WatchGlass(0.17f, 0.03f), h, new Vector3(0, 0.004f, 0), Vector3.one, Glass);
            LabFactory.MeshObject("Polvo", MeshKit.Pile(seed, 0.12f, 0.07f), h, new Vector3(0, 0.01f, 0), Vector3.one, Metal(s.Color * 0.7f, Mathf.Min(0.3f, s.Metallic), s.Smoothness * 0.5f));
            if (s.Has("pastilla"))
            {
                LabFactory.Shape("Pastilla", PrimitiveType.Cylinder, h, new Vector3(0.2f, 0.03f, 0.06f), new Vector3(0.07f, 0.03f, 0.07f), Metal(s.Color, 1f, 0.75f));
                LabFactory.MeshObject("Esfera", MeshKit.Sphere(), h, new Vector3(0.2f, 0.035f, -0.08f), Vector3.one * 0.035f, Metal(s.Color * 1.1f, 1f, 0.9f));
            }
        }

        static void YellowCrystals(Transform h, SampleInfo s, int seed)
        {
            Piece("Azufre", MeshKit.CrystalCluster(seed, 14, 0.32f, false, 4), h, new Vector3(0, 0.02f, 0), Quaternion.identity, 1f, Main(s));
            Piece("Base", MeshKit.Rock(seed + 4, new Vector3(0.2f, 0.06f, 0.15f), 0.35f, 2, true), h, new Vector3(0, 0.03f, 0), Quaternion.identity, 1f,
                Metal(s.Color * 0.85f, 0f, 0.2f));
        }

        static void Carbon(Transform h, SampleInfo s, int seed)
        {
            Piece("Grafito", MeshKit.Rock(seed, new Vector3(0.14f, 0.1f, 0.11f), 0.3f, 2, true, 0.5f), h, new Vector3(-0.05f, 0.09f, 0), Quaternion.Euler(0, 20, 0), 1f, Main(s));
            // Diamante: octaedro transparente y muy brillante
            var b = new MeshKit.Builder { OutFrom = Vector3.zero };
            var top = new Vector3(0, 0.045f, 0); var bot = new Vector3(0, -0.045f, 0);
            var ring = new[] { new Vector3(0.045f, 0, 0), new Vector3(0, 0, 0.045f), new Vector3(-0.045f, 0, 0), new Vector3(0, 0, -0.045f) };
            for (int i = 0; i < 4; i++)
            {
                b.FlatTri(ring[i], top, ring[(i + 1) % 4]);
                b.FlatTri(ring[i], ring[(i + 1) % 4], bot);
            }
            var diamond = LabFactory.MeshObject("Diamante", b.ToMesh("Diamante"), h, new Vector3(0.15f, 0.05f, 0.05f), Vector3.one, Transparent(new Color(0.92f, 0.97f, 1f), 0.55f, 0.35f));
            diamond.AddComponent<Spin>().degreesPerSecond = new Vector3(0, 60, 0);
            AddLight(h, new Vector3(0.15f, 0.2f, -0.05f), Color.white, 0.6f, 0.6f);
        }

        static void Iodine(Transform h, SampleInfo s, int seed)
        {
            var mat = Main(s);
            var rnd = new System.Random(seed);
            for (int i = 0; i < 9; i++)
            {
                var pos = new Vector3(((float)rnd.NextDouble() - 0.5f) * 0.2f, 0.01f + (float)rnd.NextDouble() * 0.02f, ((float)rnd.NextDouble() - 0.5f) * 0.14f) + new Vector3(-0.06f, 0, 0);
                Piece("Escama", MeshKit.Flake(seed + i, 0.03f + (float)rnd.NextDouble() * 0.015f, 0.008f), h, pos,
                    Quaternion.Euler((float)rnd.NextDouble() * 40, (float)rnd.NextDouble() * 360, (float)rnd.NextDouble() * 40), 1f, mat);
            }
            // Balon con vapor violeta
            LabFactory.MeshObject("Balon", LabFactory.LoadMesh("Balon"), h, new Vector3(0.14f, 0, 0.02f), Vector3.one * 1.6f, Glass);
            LabFactory.Shape("Vapor", PrimitiveType.Sphere, h, new Vector3(0.14f, 0.075f, 0.02f), Vector3.one * 0.12f, Transparent(s.Color2 ?? new Color(0.55f, 0.2f, 0.8f), 0.55f, 0.3f));
        }

        static void Mercury(Transform h, SampleInfo s, int seed)
        {
            LabFactory.Shape("Plato", PrimitiveType.Cylinder, h, new Vector3(0, 0.006f, 0), new Vector3(0.42f, 0.006f, 0.42f), LabFactory.Tinted("Card", new Color(0.55f, 0.5f, 0.45f)));
            var mat = Main(s);
            var rnd = new System.Random(seed);
            Piece("Charco", MeshKit.Puddle(seed, 0.09f, 0.022f), h, new Vector3(-0.03f, 0.014f, 0), Quaternion.identity, 1f, mat);
            for (int i = 0; i < 9; i++)
            {
                float r = 0.008f + (float)rnd.NextDouble() * 0.022f;
                var pos = new Vector3(((float)rnd.NextDouble() - 0.5f) * 0.32f, 0.012f + r * 0.4f, ((float)rnd.NextDouble() - 0.5f) * 0.3f);
                Piece("Gota", MeshKit.Puddle(seed + i + 1, r, r * 0.6f), h, pos, Quaternion.identity, 1f, mat);
            }
        }

        static void Glowing(Transform h, SampleInfo s, int seed)
        {
            var glowColor = s.Color2 ?? new Color(0.5f, 0.7f, 1f);
            float sc = Size(s);
            // Frasco pequeno con la muestra brillando en el fondo
            LabFactory.MeshObject("Frasco", LabFactory.LoadMesh("Vial"), h, Vector3.zero, Vector3.one * 1.4f, ThinGlass);
            LabFactory.Shape("Tapa", PrimitiveType.Cylinder, h, new Vector3(0, 0.28f, 0), new Vector3(0.12f, 0.03f, 0.12f), LabFactory.Tinted("Card", new Color(0.08f, 0.08f, 0.09f)));
            Piece("Muestra", MeshKit.Rock(seed, new Vector3(0.05f, 0.03f, 0.04f) * sc, 0.3f, 1, true), h, new Vector3(0, 0.035f, 0), Quaternion.identity, 1f, GlowMetal(s));
            LabFactory.Shape("Brillo", PrimitiveType.Sphere, h, new Vector3(0, 0.05f, 0), Vector3.one * 0.13f, Transparent(glowColor, 0.4f, 0.7f));
            LabFactory.Shape("Nucleo", PrimitiveType.Sphere, h, new Vector3(0, 0.045f, 0), Vector3.one * 0.06f, Glow(glowColor, 1.0f));
            AddLight(h, new Vector3(0, 0.1f, 0), glowColor, 1.4f, 1.2f);
        }

        static void Unseen(Transform h, SampleInfo s)
        {
            var glow = LabFactory.Shape("Atomos", PrimitiveType.Sphere, h, new Vector3(0, 0.18f, 0), Vector3.one * 0.06f, Glow(s.Color, 1.05f));
            glow.AddComponent<Spin>().degreesPerSecond = new Vector3(40, 60, 0);
            for (int i = 0; i < 3; i++)
            {
                var orbit = LabFactory.Node("Orbita", h, new Vector3(0, 0.18f, 0), Quaternion.Euler(60 * i, 30 * i, 0));
                LabFactory.Circle("Anillo", orbit.transform, 0.12f, 0.004f, new Color(1f, 0.5f, 0.5f, 0.8f), 48);
                orbit.AddComponent<Spin>().degreesPerSecond = new Vector3(0, 90 + 30 * i, 0);
            }
            LabFactory.Text(h, "?", new Vector3(0, 0.42f, 0), 0.14f, Palette.Warn, TextAlignmentOptions.Center, 0.3f, 0.2f, true)
                .gameObject.AddComponent<FaceCamera>();
        }

        // =================================================================== ficha con la foto real
        static void BuildInfoCard(Transform anchor, ElementInfo e, SampleInfo s)
        {
            var photo = ElementExtras.Photo(e.Z);
            var card = LabFactory.Node("Ficha", anchor, new Vector3(0, 2.45f, 0)).transform;
            card.gameObject.AddComponent<FaceCamera>();
            float w = 1.25f;
            float photoH = photo != null ? 0.62f : 0f;
            float textH = 0.5f;
            float total = 0.16f + photoH + (photo != null ? 0.12f : 0) + textH;
            float top = total / 2f;
            LabFactory.Box("Fondo", card, new Vector3(0, 0, 0.02f), new Vector3(w, total, 0.02f), LabFactory.Mat("Screen"));
            LabFactory.Box("Borde", card, new Vector3(0, top + 0.006f, 0.02f), new Vector3(w, 0.012f, 0.025f), LabFactory.Tinted("GlowBase", Palette.Accent2, 1.5f));
            LabFactory.Text(card, "<b>ASÍ SE VE EN LA REALIDAD</b>", new Vector3(0, top - 0.07f, 0), 0.06f, Palette.Accent2, TextAlignmentOptions.Center, w - 0.06f, 0.1f);
            float y = top - 0.15f;
            if (photo != null)
            {
                float aspect = photo.width / (float)Mathf.Max(1, photo.height);
                float ph = photoH, pw = ph * aspect;
                if (pw > w - 0.1f) { pw = w - 0.1f; ph = pw / aspect; }
                var quad = LabFactory.Shape("Foto", PrimitiveType.Quad, card, new Vector3(0, y - photoH / 2, 0.005f), new Vector3(pw, ph, 1f), PhotoMaterial(photo));
                quad.name = "FotoReal";
                y -= photoH;
                var cap = LabFactory.Text(card, $"{s.PhotoCaption}\n<size=70%><color=#7F93AD>{ElementExtras.PhotoCredit(e.Z)}</color></size>",
                    new Vector3(0, y - 0.06f, 0), 0.034f, Palette.TextDim, TextAlignmentOptions.Center, w - 0.08f, 0.12f);
                cap.enableAutoSizing = true; cap.fontSizeMax = 0.34f; cap.fontSizeMin = 0.2f;
                y -= 0.12f;
            }
            var looks = LabFactory.Text(card, s.Looks + "\n<size=75%><color=#9FB4CC>(El modelo 3D está ampliado; la muestra real mide unos centímetros.)</color></size>",
                new Vector3(0, y - textH / 2, 0), 0.05f, Color.white, TextAlignmentOptions.Center, w - 0.1f, textH - 0.04f);
            looks.enableAutoSizing = true; looks.fontSizeMax = 0.5f; looks.fontSizeMin = 0.3f;
        }

        public static Material PhotoMaterial(Texture tex)
        {
            var m = new Material(LabFactory.Mat("Photo")) { name = "Foto_" + tex.name, mainTexture = tex };
            return m;
        }
    }
}

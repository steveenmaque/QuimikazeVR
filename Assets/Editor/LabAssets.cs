using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LabQuimicaVR.EditorTools
{
    /// <summary>Crea texturas, materiales y mallas del laboratorio como assets.</summary>
    public static class LabAssets
    {
        public const string MatDir = "Assets/Resources/LabMaterials";
        public const string TintDir = MatDir + "/Tinted";
        public const string MeshDir = "Assets/Resources/LabMeshes";
        public const string TexDir = "Assets/Laboratorio/Texturas";

        public static void EnsureFolders()
        {
            foreach (var dir in new[] { MatDir, TintDir, MeshDir, TexDir, "Assets/Scenes" })
            {
                if (AssetDatabase.IsValidFolder(dir)) continue;
                var parts = dir.Split('/');
                string path = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    string next = path + "/" + parts[i];
                    if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(path, parts[i]);
                    path = next;
                }
            }
        }

        public static void PersistTinted(Material m)
        {
            string path = $"{TintDir}/{m.name}.mat";
            if (File.Exists(path)) return;
            AssetDatabase.CreateAsset(m, path);
        }

        // ---------------- Texturas ----------------
        public static Texture2D FloorTexture, SoftDot, HexPattern;

        public static void CreateTextures()
        {
            FloorTexture = SaveTexture("PisoCuadricula", 512, (x, y) =>
            {
                float c = 0.9f;
                bool major = x % 256 < 3 || y % 256 < 3;
                bool minor = x % 64 < 1 || y % 64 < 1;
                if (major) c = 0.74f;
                else if (minor) c = 0.85f;
                return new Color(c, c * 1.01f, c * 1.03f, 1f);
            }, TextureWrapMode.Repeat);

            HexPattern = SaveTexture("PatronHexagonal", 512, HexPatternPixel, TextureWrapMode.Repeat, true);

            SoftDot = SaveTexture("PuntoSuave", 64, (x, y) =>
            {
                float dx = (x + 0.5f) / 32f - 1f, dy = (y + 0.5f) / 32f - 1f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(1f - d);
                a = a * a * (3f - 2f * a);
                return new Color(1, 1, 1, a);
            }, TextureWrapMode.Clamp, true);
        }

        /// <summary>
        /// Patron de anillos hexagonales con un "atomo" en cada vertice (como el grafeno o el benceno).
        /// La textura cubre una celda de 3R x raiz(3)R estirada a un cuadrado; el material corrige la proporcion.
        /// </summary>
        static Color HexPatternPixel(int x, int y)
        {
            const float a = 0.8660254f; // apotema de un hexagono de radio 1
            var p = new Vector2((x + 0.5f) / 512f * 3f, (y + 0.5f) / 512f * 2f * a);
            Vector2 best = Vector2.zero;
            float bestD = float.MaxValue;
            for (int i = -1; i <= 2; i++)
                for (int j = -1; j <= 1; j++)
                {
                    var c = new Vector2(1.5f * i, 2f * a * (j + ((i & 1) != 0 ? 0.5f : 0f)));
                    float d = (p - c).sqrMagnitude;
                    if (d < bestD) { bestD = d; best = c; }
                }
            var q = p - best;
            q = new Vector2(Mathf.Abs(q.x), Mathf.Abs(q.y));
            float edge = a - Mathf.Max(q.y, a * q.x + 0.5f * q.y);
            float line = 1f - Smooth(0.025f, 0.05f, edge);
            float vd = Mathf.Min((q - new Vector2(1f, 0f)).magnitude, (q - new Vector2(0.5f, a)).magnitude);
            float dot = 1f - Smooth(0.08f, 0.12f, vd);
            return new Color(1f, 1f, 1f, Mathf.Max(line * 0.65f, dot));
        }

        /// <summary>smoothstep de los shaders (Mathf.SmoothStep es otra cosa: interpola entre dos valores).</summary>
        static float Smooth(float e0, float e1, float x)
        {
            float t = Mathf.Clamp01((x - e0) / (e1 - e0));
            return t * t * (3f - 2f * t);
        }

        static Texture2D SaveTexture(string name, int size, System.Func<int, int, Color> fn, TextureWrapMode wrap, bool alpha = false)
        {
            string path = $"{TexDir}/{name}.png";
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, fn(x, y));
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.wrapMode = wrap;
            imp.mipmapEnabled = true;
            imp.alphaIsTransparency = alpha;
            imp.anisoLevel = 4;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // ---------------- Materiales ----------------
        static Shader FindShader(params string[] names)
        {
            foreach (var n in names)
            {
                var s = Shader.Find(n);
                if (s != null) return s;
            }
            Debug.LogWarning("No se encontró ningún shader de: " + string.Join(", ", names));
            return Shader.Find("Standard");
        }

        static Material GetMat(string name, Shader shader)
        {
            string path = $"{MatDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(m, path);
            }
            else if (m.shader != shader) m.shader = shader;
            return m;
        }

        static Material Standard(string name, string hex, float gloss = 0.2f, float metallic = 0f, float emission = 0f)
        {
            var m = GetMat(name, FindShader("Standard"));
            m.color = Hex(hex);
            m.SetFloat("_Glossiness", gloss);
            m.SetFloat("_Metallic", metallic);
            SetEmission(m, emission > 0 ? Hex(hex) * emission : Color.black);
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        static void SetEmission(Material m, Color c)
        {
            if (c.maxColorComponent > 0.001f)
            {
                m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else m.DisableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c);
        }

        static Material Unlit(string name, string hex)
        {
            var m = GetMat(name, FindShader("Unlit/Color"));
            m.color = Hex(hex);
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>Material de los paneles redondeados (Quimikaze/Panel) con el patron hexagonal opcional.</summary>
        static Material Panel(string name, string hex, float pattern)
        {
            var m = GetMat(name, FindShader("Quimikaze/Panel"));
            m.color = Hex(hex);
            m.SetTexture("_Pattern", HexPattern);
            // Celda del patron: hexagonos de 0.16 m de radio (3R x raiz(3)R por repeticion).
            m.SetTextureScale("_Pattern", new Vector2(1f / (3f * 0.16f), 1f / (1.7320508f * 0.16f)));
            m.SetColor("_PatternColor", new Color(0.75f, 0.8f, 1f));
            m.SetFloat("_PatternStrength", pattern);
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>Material mate barato (Lambert) para superficies grandes: mucho mas liviano que Standard en celulares.</summary>
        static Material Diffuse(string name, string hex)
        {
            var m = GetMat(name, FindShader("Legacy Shaders/Diffuse", "Mobile/Diffuse"));
            m.color = Hex(hex);
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>Configura un material Standard como transparente (mode 3) o desvanecido (mode 2).</summary>
        static Material Transparent(string name, Color color, float gloss, bool fade, float emission = 0f)
        {
            var m = GetMat(name, FindShader("Standard"));
            m.color = color;
            m.SetFloat("_Glossiness", gloss);
            m.SetFloat("_Metallic", 0f);
            m.SetFloat("_Mode", fade ? 2 : 3);
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetInt("_SrcBlend", fade ? (int)UnityEngine.Rendering.BlendMode.SrcAlpha : (int)UnityEngine.Rendering.BlendMode.One);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.DisableKeyword("_ALPHATEST_ON");
            if (fade) { m.EnableKeyword("_ALPHABLEND_ON"); m.DisableKeyword("_ALPHAPREMULTIPLY_ON"); }
            else { m.DisableKeyword("_ALPHABLEND_ON"); m.EnableKeyword("_ALPHAPREMULTIPLY_ON"); }
            m.renderQueue = 3000;
            SetEmission(m, emission > 0 ? new Color(color.r, color.g, color.b) * emission : Color.black);
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material Particle(string name, bool additive)
        {
            var shader = additive
                ? FindShader("Legacy Shaders/Particles/Additive", "Mobile/Particles/Additive", "Particles/Standard Unlit")
                : FindShader("Legacy Shaders/Particles/Alpha Blended", "Mobile/Particles/Alpha Blended", "Particles/Standard Unlit");
            var m = GetMat(name, shader);
            if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", SoftDot);
            if (m.HasProperty("_TintColor")) m.SetColor("_TintColor", new Color(0.5f, 0.5f, 0.5f, 0.5f));
            EditorUtility.SetDirty(m);
            return m;
        }

        public static void CreateMaterials()
        {
            // Sala
            // Superficies grandes con shader mate barato (se dibujan dos veces en el visor)
            Diffuse("Wall", "FAFBFD");
            Diffuse("Ceiling", "FFFFFF");
            var floor = Diffuse("Floor", "E3E7EC");
            floor.mainTexture = FloorTexture;
            Unlit("Trim", "00C8F0");
            Diffuse("TrimSoft", "DCEBF5");
            Unlit("LightPanel", "FFFFFF");
            Standard("ScreenFrame", "2A3039", 0.6f, 0.5f);
            Diffuse("BenchBody", "EEF1F4");
            Standard("BenchTop", "353C46", 0.55f);
            Diffuse("Pedestal", "E6EAEF");
            Standard("Metal", "B8BEC6", 0.75f, 0.9f);

            // Pantallas (sin iluminacion, como un monitor)
            Unlit("Card", "1C3350");
            Unlit("Screen", "1E1F5E");
            // Paneles redondeados: el color va en los vertices (degradado) y el material solo lo multiplica.
            Panel("Panel", "FFFFFF", 0.14f);
            Panel("PanelPlain", "FFFFFF", 0f);
            Panel("ButtonIdle", "5A5FD8", 0f);
            Panel("ButtonActive", "0F8A55", 0f);
            // Los botones tenidos guardados con el shader anterior se vuelven a crear.
            foreach (var old in Directory.GetFiles(TintDir, "ButtonIdle_*.mat"))
                AssetDatabase.DeleteAsset(old.Replace('\\', '/'));
            Unlit("TileHover", "00E5FF");
            Unlit("Reticle", "FFFFFF");
            Standard("GlowBase", "00C8F0", 0.3f, 0, 1.5f);

            // Tabla periodica
            foreach (var c in CategoryInfo.AllCategories)
            {
                var col = CategoryInfo.Color(c);
                Diffuse("Cat_" + CategoryInfo.Code(c), ColorUtility.ToHtmlStringRGB(col));
            }
            Diffuse("TileDim", "4A5260");

            // Atomos
            Standard("Proton", "E53935", 0.6f, 0, 0.25f);
            Standard("Neutron", "90A4AE", 0.6f, 0, 0.1f);
            Standard("Electron", "29B6F6", 0.8f, 0, 1.6f);
            Standard("Bond", "CFD8DC", 0.5f);
            Standard("Atom", "FFFFFF", 0.6f);
            Standard("Sample", "C9CCD2", 0.8f, 1f);

            // Vidrio, liquidos, gases y efectos
            Transparent("Glass", new Color(0.82f, 0.93f, 1f, 0.22f), 0.95f, false);
            Transparent("GasGlow", new Color(1f, 1f, 1f, 0.3f), 0.2f, true, 0.5f);
            Transparent("Hologram", new Color(0.2f, 0.85f, 1f, 0.09f), 0.1f, true, 0.8f);
            Standard("LiquidBlue", "29B6F6", 0.9f, 0, 0.35f);
            Standard("LiquidGreen", "66BB6A", 0.9f, 0, 0.35f);
            Standard("LiquidPink", "EC407A", 0.9f, 0, 0.35f);
            Standard("LiquidAmber", "FFB300", 0.9f, 0, 0.35f);
            Standard("LiquidViolet", "7E57C2", 0.9f, 0, 0.35f);

            var line = GetMat("Line", FindShader("Sprites/Default"));
            line.color = Color.white;
            EditorUtility.SetDirty(line);
            Particle("CloudParticle", false);
            Particle("Spark", true);

            // Fotos reales, retratos y paisajes 360 (sin iluminacion, se ven tal como son)
            var photo = GetMat("Photo", FindShader("Unlit/Texture"));
            EditorUtility.SetDirty(photo);
            var pano = GetMat("Panorama", FindShader("Unlit/Texture"));
            EditorUtility.SetDirty(pano);
            // El paisaje de la transicion se desvanece y se dibuja encima de todo: el shader de UI
            // tiene color con alfa y permite desactivar la prueba de profundidad (unity_GUIZTestMode).
            var fade = GetMat("PanoramaFade", FindShader("UI/Default"));
            fade.color = Color.white;
            EditorUtility.SetDirty(fade);
            AssetDatabase.SaveAssets();
        }

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            return c;
        }

        // ---------------- Mallas ----------------
        public static void CreateMeshes()
        {
            // Tubo de ensayo
            SaveMesh("Vial", Lathe(new[] { V(0, 0), V(0.03f, 0.006f), V(0.05f, 0.022f), V(0.06f, 0.05f), V(0.06f, 0.34f), V(0.066f, 0.345f) }));
            // Cupula de vidrio (semiesfera abierta abajo)
            var dome = new List<Vector2>();
            for (int i = 0; i <= 12; i++) { float a = i / 12f * Mathf.PI / 2f; dome.Add(V(Mathf.Cos(a), Mathf.Sin(a))); }
            SaveMesh("Dome", Lathe(dome.ToArray()));
            // Matraz Erlenmeyer y su liquido
            SaveMesh("Erlenmeyer", Lathe(new[] { V(0, 0), V(0.09f, 0), V(0.092f, 0.01f), V(0.03f, 0.16f), V(0.03f, 0.24f), V(0.036f, 0.245f) }));
            SaveMesh("LiquidoCono", Lathe(new[] { V(0, 0.004f), V(0.086f, 0.004f), V(0.062f, 0.065f), V(0, 0.065f) }));
            // Vaso de precipitado y su liquido
            SaveMesh("Vaso", Lathe(new[] { V(0, 0), V(0.065f, 0), V(0.065f, 0.15f), V(0.072f, 0.156f) }));
            SaveMesh("LiquidoVaso", Lathe(new[] { V(0, 0.004f), V(0.06f, 0.004f), V(0.06f, 0.08f), V(0, 0.08f) }));
            // Balon de fondo redondo
            var flask = new List<Vector2>();
            for (int i = 0; i <= 14; i++)
            {
                float a = -Mathf.PI / 2f + i / 14f * Mathf.PI * 0.88f;
                flask.Add(V(Mathf.Cos(a) * 0.08f, 0.08f + Mathf.Sin(a) * 0.08f));
            }
            flask.Add(V(0.025f, 0.17f)); flask.Add(V(0.025f, 0.28f)); flask.Add(V(0.03f, 0.285f));
            SaveMesh("Balon", Lathe(flask.ToArray()));
            SaveMesh("LiquidoBalon", Lathe(new[] { V(0, 0.004f), V(0.05f, 0.02f), V(0.072f, 0.05f), V(0, 0.05f) }));
            // Haz holografico (cono truncado)
            SaveMesh("Haz", Lathe(new[] { V(0.5f, 0), V(1.25f, 2.2f) }));
            // Anillo plano para el piso y la reticula
            SaveMesh("Anillo", Ring(0.86f, 1f, 64));
            // Esfera de pocos poligonos (diametro 1, como la de Unity) para protones, neutrones y electrones
            var esf = new List<Vector2>();
            for (int i = 0; i <= 6; i++)
            {
                float a = -Mathf.PI / 2f + i / 6f * Mathf.PI;
                esf.Add(V(i == 0 || i == 6 ? 0f : Mathf.Cos(a) * 0.5f, Mathf.Sin(a) * 0.5f));
            }
            var esfera = Lathe(esf.ToArray(), 12);
            var normals = esfera.vertices;
            for (int i = 0; i < normals.Length; i++) normals[i] = normals[i].normalized;
            esfera.normals = normals;
            SaveMesh("EsferaBaja", esfera);
            AssetDatabase.SaveAssets();
        }

        static Vector2 V(float r, float y) => new Vector2(r, y);

        static void SaveMesh(string name, Mesh mesh)
        {
            string path = $"{MeshDir}/{name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            mesh.name = name;
            if (existing != null)
            {
                existing.Clear();
                EditorUtility.CopySerialized(mesh, existing);
                EditorUtility.SetDirty(existing);
            }
            else AssetDatabase.CreateAsset(mesh, path);
        }

        /// <summary>Malla de revolucion a partir de un perfil (radio, altura).</summary>
        static Mesh Lathe(Vector2[] profile, int segments = 40)
        {
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            int rows = profile.Length;
            for (int s = 0; s <= segments; s++)
            {
                float a = s / (float)segments * Mathf.PI * 2f;
                float cos = Mathf.Cos(a), sin = Mathf.Sin(a);
                for (int i = 0; i < rows; i++)
                {
                    verts.Add(new Vector3(profile[i].x * cos, profile[i].y, profile[i].x * sin));
                    uvs.Add(new Vector2(s / (float)segments, i / (float)(rows - 1)));
                }
            }
            float minY = float.MaxValue, maxY = float.MinValue;
            foreach (var p in profile) { minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y); }
            float midY = (minY + maxY) / 2f;
            for (int s = 0; s < segments; s++)
            {
                for (int i = 0; i < rows - 1; i++)
                {
                    int a = s * rows + i, b = a + 1, c = a + rows, d = c + 1;
                    AddOutward(verts, tris, a, b, d, midY);
                    AddOutward(verts, tris, a, d, c, midY);
                }
            }
            var mesh = new Mesh();
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Agrega el triangulo con la cara mirando hacia afuera del eje (respecto a la mitad de su altura).</summary>
        static void AddOutward(List<Vector3> v, List<int> tris, int a, int b, int c, float midY)
        {
            var n = Vector3.Cross(v[b] - v[a], v[c] - v[a]);
            if (n.sqrMagnitude < 1e-14f) return;
            var center = (v[a] + v[b] + v[c]) / 3f;
            var outward = center - new Vector3(0, midY, 0);
            if (Vector3.Dot(n, outward) < 0) { tris.Add(a); tris.Add(c); tris.Add(b); }
            else { tris.Add(a); tris.Add(b); tris.Add(c); }
        }

        static Mesh Ring(float inner, float outer, int segments)
        {
            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var tris = new List<int>();
            // Dos caras con vertices propios (arriba y abajo) para que se ilumine bien desde ambos lados.
            foreach (var up in new[] { 1f, -1f })
            {
                int start = verts.Count;
                for (int s = 0; s <= segments; s++)
                {
                    float a = s / (float)segments * Mathf.PI * 2f;
                    var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                    verts.Add(d * inner); normals.Add(Vector3.up * up);
                    verts.Add(d * outer); normals.Add(Vector3.up * up);
                }
                for (int s = 0; s < segments; s++)
                {
                    int i0 = start + s * 2, o0 = i0 + 1, i1 = i0 + 2, o1 = i0 + 3;
                    if (up > 0) tris.AddRange(new[] { i0, i1, o1, i0, o1, o0 });
                    else tris.AddRange(new[] { i0, o1, i1, i0, o0, o1 });
                }
            }
            var mesh = new Mesh();
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}

using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace LabQuimicaVR
{
    /// <summary>
    /// Ayudantes para crear piezas del laboratorio (cajas, textos, pantallas y botones).
    /// Funcionan igual en el editor (para construir las escenas) y en ejecucion.
    /// </summary>
    public static class LabFactory
    {
        static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
        static readonly Dictionary<PrimitiveType, Mesh> primitives = new Dictionary<PrimitiveType, Mesh>();
        static readonly Dictionary<string, Material> tinted = new Dictionary<string, Material>();

        public static Material Mat(string name)
        {
            if (materials.TryGetValue(name, out var m) && m != null) return m;
            m = Resources.Load<Material>("LabMaterials/" + name);
            if (m == null)
            {
                Debug.LogWarning($"Material '{name}' no encontrado en Resources/LabMaterials");
                m = new Material(Shader.Find("Standard"));
            }
            materials[name] = m;
            return m;
        }

        /// <summary>Copia de un material base con otro color (se reutiliza por color).</summary>
        public static Material Tinted(string baseName, Color color, float emission = 0f)
        {
            string key = $"{baseName}_{ColorUtility.ToHtmlStringRGBA(color)}_{Mathf.RoundToInt(emission * 100)}";
            if (tinted.TryGetValue(key, out var m) && m != null) return m;
            m = Resources.Load<Material>("LabMaterials/Tinted/" + key);
            if (m != null) { tinted[key] = m; return m; }
            m = new Material(Mat(baseName)) { name = key };
            m.color = color;
            if (m.HasProperty("_EmissionColor"))
            {
                if (emission > 0f)
                {
                    m.EnableKeyword("_EMISSION");
                    m.SetColor("_EmissionColor", color * emission);
                }
            }
            tinted[key] = m;
            if (!Application.isPlaying) PersistHook?.Invoke(m);
            return m;
        }

        /// <summary>El constructor de escenas (editor) lo usa para guardar como asset los materiales creados.</summary>
        public static System.Action<Material> PersistHook;

        public static Mesh Primitive(PrimitiveType type)
        {
            if (primitives.TryGetValue(type, out var mesh) && mesh != null) return mesh;
            var go = GameObject.CreatePrimitive(type);
            mesh = go.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(go);
            primitives[type] = mesh;
            return mesh;
        }

        public static Mesh LoadMesh(string name) => Resources.Load<Mesh>("LabMeshes/" + name);

        public static GameObject Node(string name, Transform parent, Vector3 localPos = default, Quaternion? localRot = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot ?? Quaternion.identity;
            return go;
        }

        public static GameObject Shape(string name, PrimitiveType type, Transform parent, Vector3 localPos, Vector3 scale, Material mat, bool collider = false)
        {
            return MeshObject(name, Primitive(type), parent, localPos, scale, mat, collider ? type : (PrimitiveType?)null);
        }

        public static GameObject Box(string name, Transform parent, Vector3 localPos, Vector3 size, Material mat, bool collider = false)
            => Shape(name, PrimitiveType.Cube, parent, localPos, size, mat, collider);

        public static GameObject MeshObject(string name, Mesh mesh, Transform parent, Vector3 localPos, Vector3 scale, Material mat, PrimitiveType? colliderType = null)
        {
            var go = Node(name, parent, localPos);
            go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (colliderType.HasValue)
            {
                switch (colliderType.Value)
                {
                    case PrimitiveType.Sphere: go.AddComponent<SphereCollider>(); break;
                    case PrimitiveType.Capsule: go.AddComponent<CapsuleCollider>(); break;
                    default: go.AddComponent<BoxCollider>(); break;
                }
            }
            return go;
        }

        /// <summary>
        /// Texto 3D. "size" es la altura de la letra en metros (aprox.).
        /// El texto se lee mirando hacia +Z del padre.
        /// </summary>
        public static TextMeshPro Text(Transform parent, string text, Vector3 localPos, float size, Color color,
            TextAlignmentOptions align = TextAlignmentOptions.Center, float width = 4f, float height = 1f, bool bold = false,
            TextRole role = TextRole.Auto)
        {
            var go = new GameObject("Texto", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshPro>();
            tmp.rectTransform.sizeDelta = new Vector2(width, height);
            tmp.rectTransform.localPosition = localPos;
            tmp.text = text;
            tmp.fontSize = size * 10f;
            tmp.color = color;
            tmp.alignment = align;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.richText = true;
            if (bold) tmp.fontStyle = FontStyles.Bold;
            LabType.Apply(tmp, role, bold);
            var r = tmp.GetComponent<MeshRenderer>();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return tmp;
        }

        /// <summary>
        /// Pantalla con esquinas redondeadas: marco del color de acento, fondo violeta-azul con degradado y patron
        /// de anillos hexagonales, y el titulo en una cinta de color. Devuelve la raiz; el contenido se coloca
        /// en coordenadas locales (x derecha, y arriba) con z = 0 en la superficie de la pantalla.
        /// </summary>
        public static Transform Screen(string name, Transform parent, Vector3 localPos, Quaternion localRot, Vector2 size,
            string title = null, Color? accent = null)
        {
            var root = Node(name, parent, localPos, localRot).transform;
            var acc = accent ?? Palette.Accent;
            acc.a = 1f;
            float radius = Mathf.Clamp(Mathf.Min(size.x, size.y) * 0.06f, 0.08f, 0.3f);
            Panel("Marco", root, new Vector3(0, 0, 0.06f), size + new Vector2(0.2f, 0.2f), radius + 0.1f, 0.08f,
                Color.Lerp(acc, Color.white, 0.3f), Color.Lerp(acc, Color.black, 0.45f));
            Panel("Pantalla", root, new Vector3(0, 0, 0.015f), size, radius, 0.02f,
                Color.Lerp(Palette.PanelTop, acc, 0.12f), Palette.PanelBottom, "Panel");
            if (!string.IsNullOrEmpty(title))
            {
                float th = Mathf.Clamp(size.y * 0.075f, 0.12f, 0.3f);
                float y = size.y / 2 - th * 0.85f;
                var ink = Luma(acc) > 0.45f ? Palette.Ink : Color.white;
                var t = Text(root, title, new Vector3(0, y, -0.02f), th, ink, TextAlignmentOptions.Center, size.x - 0.5f, th * 1.6f, true);
                t.textWrappingMode = TextWrappingModes.NoWrap;
                t.enableAutoSizing = true;
                t.fontSizeMax = th * 10f;
                t.fontSizeMin = th * 5f;
                t.ForceMeshUpdate();
                float w = t.textBounds.size.x;
                if (!(w > 0.05f) || w > size.x) w = size.x * 0.6f;
                // Cinta del titulo (como las etiquetas de los juegos): del color de acento con brillo arriba.
                Panel("Cinta", root, new Vector3(0, y, -0.004f), new Vector2(Mathf.Min(w + th * 1.6f, size.x - 0.2f), th * 1.3f), th * 0.6f, 0.02f,
                    Color.Lerp(acc, Color.white, 0.35f), acc);
            }
            return root;
        }

        /// <summary>Rectangulo redondeado con degradado vertical (colores en los vertices).</summary>
        public static GameObject Panel(string name, Transform parent, Vector3 localPos, Vector2 size, float radius, float depth,
            Color top, Color bottom, string material = "PanelPlain")
        {
            top.a = bottom.a = 1f;
            return MeshObject(name, PanelMesh.Rounded(size, radius, depth, top, bottom), parent, localPos, Vector3.one, Mat(material));
        }

        public static float Luma(Color c) => 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;

        /// <summary>Tarjeta redondeada (un poco mas clara arriba) dentro de una pantalla.</summary>
        public static Transform Card(Transform parent, Vector3 localPos, Vector2 size, Color color)
        {
            float radius = Mathf.Min(0.09f, Mathf.Min(size.x, size.y) * 0.25f);
            var go = Panel("Tarjeta", parent, localPos + new Vector3(0, 0, -0.005f), size, radius, 0.008f,
                Color.Lerp(color, Color.white, 0.14f), Color.Lerp(color, Color.black, 0.12f));
            return go.transform;
        }

        public static GazeButton Button(Transform parent, string label, Vector3 localPos, Vector2 size, float textSize = 0.12f,
            Color? color = null, TextRole role = TextRole.Title)
        {
            var root = Node("Boton_" + label, parent, localPos);
            var col = root.AddComponent<BoxCollider>();
            col.size = new Vector3(size.x, size.y, 0.08f);
            var idle = color.HasValue ? Tinted("ButtonIdle", color.Value, 0.25f) : Mat("ButtonIdle");
            // Pildora con brillo arriba: el material pone el color (normal, mirado o elegido) y la malla el degradado.
            float radius = Mathf.Min(size.x, size.y) * 0.42f;
            var body = MeshObject("Fondo", PanelMesh.Rounded(size, radius, 0.04f, Color.white, new Color(0.7f, 0.7f, 0.74f)),
                root.transform, new Vector3(0, 0, 0.01f), Vector3.one, idle);
            var outline = MeshObject("Contorno", PanelMesh.Rounded(size + new Vector2(0.06f, 0.06f), radius + 0.03f, 0.02f, Color.white, Color.white),
                root.transform, new Vector3(0, 0, 0.025f), Vector3.one, Mat("TileHover"));
            outline.SetActive(false);
            var text = Text(root.transform, label, new Vector3(0, 0, -0.02f), textSize, Color.white, TextAlignmentOptions.Center, size.x - 0.04f, size.y, true, role);
            // En los botones, sombra en vez de halo: el texto pequeno se lee mas nitido.
            if (text.font == LabType.Title) text.fontSharedMaterial = LabType.Shadow(text.font);
            var b = root.AddComponent<GazeButton>();
            b.Setup(body.GetComponent<Renderer>(), outline, text, idle);
            return b;
        }

        public static void ClearChildren(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                var c = t.GetChild(i).gameObject;
                if (Application.isPlaying) { c.SetActive(false); Object.Destroy(c); }
                else Object.DestroyImmediate(c);
            }
        }

        /// <summary>Anillo plano (para trazar orbitas o marcas en el piso) hecho con LineRenderer.</summary>
        public static LineRenderer Circle(string name, Transform parent, float radius, float width, Color color, int segments = 72)
        {
            var go = Node(name, parent);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.loop = true;
            lr.positionCount = segments;
            lr.widthMultiplier = width;
            lr.sharedMaterial = Mat("Line");
            lr.startColor = lr.endColor = color;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                lr.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, 0, Mathf.Sin(a) * radius));
            }
            return lr;
        }
    }

    /// <summary>
    /// Colores de Quimikaze (ver COLORES.md): fondos violeta-azul saturados pero oscuros (menos fatiga en VR y
    /// mas agradables para ninos que el gris o el negro), acentos brillantes y texto claro con contraste 7:1 o mas.
    /// </summary>
    public static class Palette
    {
        public static readonly Color Accent = new Color(0.0f, 0.78f, 0.95f);
        public static readonly Color Accent2 = new Color(1.0f, 0.75f, 0.2f);
        public static readonly Color Pink = new Color(1.0f, 0.42f, 0.7f);
        public static readonly Color Violet = new Color(0.62f, 0.45f, 1.0f);
        public static readonly Color Text = new Color(0.95f, 0.96f, 1f);
        public static readonly Color TextDim = new Color(0.8f, 0.83f, 1f);
        public static readonly Color Ink = new Color(0.11f, 0.08f, 0.27f);
        public static readonly Color PanelTop = new Color(0.25f, 0.18f, 0.56f);
        public static readonly Color PanelBottom = new Color(0.09f, 0.14f, 0.42f);
        public static readonly Color CardBg = new Color(0.27f, 0.29f, 0.68f);
        public static readonly Color ButtonAlt = new Color(0.47f, 0.33f, 0.8f);
        public static readonly Color Good = new Color(0.35f, 0.9f, 0.5f);
        public static readonly Color Warn = new Color(1f, 0.55f, 0.3f);
    }
}

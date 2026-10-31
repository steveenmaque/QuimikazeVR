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
        /// Pantalla oscura con marco luminoso y titulo. Devuelve la raiz; el contenido se coloca
        /// en coordenadas locales (x derecha, y arriba) con z = 0 en la superficie de la pantalla.
        /// </summary>
        public static Transform Screen(string name, Transform parent, Vector3 localPos, Quaternion localRot, Vector2 size,
            string title = null, Color? accent = null)
        {
            var root = Node(name, parent, localPos, localRot).transform;
            var acc = accent ?? Palette.Accent;
            Box("Marco", root, new Vector3(0, 0, 0.06f), new Vector3(size.x + 0.16f, size.y + 0.16f, 0.08f), Mat("ScreenFrame"));
            Box("Pantalla", root, new Vector3(0, 0, 0.015f), new Vector3(size.x, size.y, 0.02f), Mat("Screen"));
            var glow = Tinted("GlowBase", acc, 1.6f);
            Box("BordeSup", root, new Vector3(0, size.y / 2 + 0.04f, 0f), new Vector3(size.x + 0.1f, 0.025f, 0.03f), glow);
            Box("BordeInf", root, new Vector3(0, -size.y / 2 - 0.04f, 0f), new Vector3(size.x + 0.1f, 0.025f, 0.03f), glow);
            if (!string.IsNullOrEmpty(title))
            {
                float th = Mathf.Clamp(size.y * 0.075f, 0.12f, 0.3f);
                var t = Text(root, title, new Vector3(0, size.y / 2 - th * 0.95f, -0.01f), th, acc,
                    TextAlignmentOptions.Center, size.x - 0.3f, th * 1.6f, true);
                t.textWrappingMode = TextWrappingModes.NoWrap;
                t.enableAutoSizing = true;
                t.fontSizeMax = th * 10f;
                t.fontSizeMin = th * 5f;
                Box("Linea", root, new Vector3(0, size.y / 2 - th * 1.85f, -0.005f), new Vector3(size.x * 0.92f, 0.012f, 0.005f), glow);
            }
            return root;
        }

        /// <summary>Forma redondeada con degradado vertical (iconos y etiquetas); los colores van en los vertices.</summary>
        public static GameObject Panel(string name, Transform parent, Vector3 localPos, Vector2 size, float radius, float depth,
            Color top, Color bottom)
        {
            top.a = bottom.a = 1f;
            return MeshObject(name, PanelMesh.Rounded(size, radius, depth, top, bottom), parent, localPos, Vector3.one, Mat("PanelPlain"));
        }

        /// <summary>Tarjeta (rectangulo de color) dentro de una pantalla.</summary>
        public static Transform Card(Transform parent, Vector3 localPos, Vector2 size, Color color)
        {
            var go = Box("Tarjeta", parent, localPos + new Vector3(0, 0, -0.005f), new Vector3(size.x, size.y, 0.008f), Tinted("Card", color));
            return go.transform;
        }

        public static GazeButton Button(Transform parent, string label, Vector3 localPos, Vector2 size, float textSize = 0.12f,
            Color? color = null, TextRole role = TextRole.Title)
        {
            var root = Node("Boton_" + label, parent, localPos);
            var col = root.AddComponent<BoxCollider>();
            col.size = new Vector3(size.x, size.y, 0.08f);
            var idle = color.HasValue ? Tinted("ButtonIdle", color.Value, 0.25f) : Mat("ButtonIdle");
            var body = Box("Fondo", root.transform, new Vector3(0, 0, 0.01f), new Vector3(size.x, size.y, 0.04f), idle);
            var outline = Box("Contorno", root.transform, new Vector3(0, 0, 0.025f), new Vector3(size.x + 0.05f, size.y + 0.05f, 0.02f), Mat("TileHover"));
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

    public static class Palette
    {
        public static readonly Color Accent = new Color(0.0f, 0.78f, 0.95f);
        public static readonly Color Accent2 = new Color(1.0f, 0.75f, 0.2f);
        public static readonly Color Text = new Color(0.93f, 0.96f, 1f);
        public static readonly Color TextDim = new Color(0.62f, 0.72f, 0.84f);
        public static readonly Color Dark = new Color(0.1f, 0.12f, 0.16f);
        public static readonly Color CardBg = new Color(0.12f, 0.2f, 0.32f);
        public static readonly Color Good = new Color(0.35f, 0.9f, 0.5f);
        public static readonly Color Warn = new Color(1f, 0.55f, 0.3f);
        public static readonly Color Violet = new Color(0.62f, 0.45f, 1.0f);
        /// <summary>Tinta oscura para texto sobre iconos y etiquetas de color claro.</summary>
        public static readonly Color Ink = new Color(0.11f, 0.08f, 0.27f);
    }
}

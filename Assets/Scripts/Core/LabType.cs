using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace LabQuimicaVR
{
    /// <summary>Que papel cumple un texto: de el depende la fuente, el espaciado y el efecto.</summary>
    public enum TextRole
    {
        /// <summary>Negrita = titulo, normal = texto.</summary>
        Auto,
        /// <summary>Titulos, botones y cifras grandes (Fredoka SemiBold, redondeada y amigable).</summary>
        Title,
        /// <summary>Parrafos y etiquetas (Atkinson Hyperlegible Next Medium, letras que no se confunden).</summary>
        Body,
        /// <summary>Simbolos y formulas quimicas (Atkinson Bold: distingue Cl de CI y O de 0).</summary>
        Symbol,
    }

    /// <summary>
    /// Sistema tipografico de Quimikaze (ver TIPOGRAFIA.md):
    /// - Titulos en Fredoka con brillo de su color en pantallas oscuras, como los menus de neon de los juegos VR.
    /// - Texto en Atkinson Hyperlegible Next con peso medio (los pesos finos se leen peor en VR) y algo mas de
    ///   espacio entre letras y lineas, que ayuda a lectores de 10 a 14 anos.
    /// - Simbolos quimicos en Atkinson Bold, que diferencia l / I / 1 y O / 0.
    /// Las fuentes se generan con el menu Quimikaze/Crear fuentes en Resources/Fuentes.
    /// </summary>
    public static class LabType
    {
        public const string Dir = "Fuentes/";
        public const string TitleFont = "Fredoka SDF";
        public const string BodyFont = "Atkinson SDF";
        public const string BoldFont = "Atkinson Bold SDF";

        static TMP_FontAsset title, body, bold;
        static readonly Dictionary<string, Material> effects = new Dictionary<string, Material>();

        public static TMP_FontAsset Title => title != null ? title : title = Resources.Load<TMP_FontAsset>(Dir + TitleFont);
        public static TMP_FontAsset Body => body != null ? body : body = Resources.Load<TMP_FontAsset>(Dir + BodyFont);
        public static TMP_FontAsset Bold => bold != null ? bold : bold = Resources.Load<TMP_FontAsset>(Dir + BoldFont);

        public static void Apply(TextMeshPro t, TextRole role, bool bold)
        {
            if (role == TextRole.Auto) role = bold ? TextRole.Title : TextRole.Body;
            float size = t.fontSize / 10f;
            switch (role)
            {
                case TextRole.Title:
                    if (Title == null) break;
                    t.font = Title;
                    t.fontStyle = FontStyles.Normal;
                    // Las mayusculas necesitan mas aire entre letras.
                    t.characterSpacing = IsUpper(t.text) ? 4f : 1f;
                    // Degradado suave (luz desde arriba): el color se multiplica, asi que respeta las etiquetas <color>.
                    t.enableVertexGradient = true;
                    t.colorGradient = new VertexGradient(Color.white, Color.white, Shade, Shade);
                    if (Luma(t.color) > 0.45f)
                        t.fontSharedMaterial = size >= 0.11f ? Glow(t.color) : Shadow(Title);
                    break;
                case TextRole.Symbol:
                    if (Bold == null) break;
                    t.font = Bold;
                    t.fontStyle = FontStyles.Normal;
                    t.characterSpacing = 0f;
                    break;
                default:
                    if (Body == null) break;
                    t.font = Body;
                    t.characterSpacing = 1.5f;
                    t.wordSpacing = 6f;
                    t.lineSpacing = 4f;
                    break;
            }
        }

        static readonly Color Shade = new Color(0.8f, 0.82f, 0.88f, 1f);

        static float Luma(Color c) => 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;

        static bool IsUpper(string s)
        {
            int letters = 0;
            bool inTag = false;
            foreach (char ch in s)
            {
                if (ch == '<') inTag = true;
                else if (ch == '>') inTag = false;
                else if (!inTag && char.IsLetter(ch))
                {
                    if (char.IsLower(ch)) return false;
                    letters++;
                }
            }
            return letters > 1;
        }

        /// <summary>Texto que flota sobre fotos, paisajes o la sala: sombra oscura ancha para que siempre contraste.</summary>
        public static void OverImage(TextMeshPro t) => t.fontSharedMaterial = Shadow(t.font, true);

        /// <summary>Halo de neon del mismo color que el titulo (pantallas oscuras).</summary>
        public static Material Glow(Color color)
        {
            var c = color; c.a = 1f;
            string key = "TMP_Brillo_" + ColorUtility.ToHtmlStringRGB(c);
            return Effect(key, Title, m =>
            {
                m.EnableKeyword("UNDERLAY_ON");
                m.SetColor("_UnderlayColor", new Color(c.r, c.g, c.b, 0.42f));
                m.SetFloat("_UnderlayOffsetX", 0f);
                m.SetFloat("_UnderlayOffsetY", 0f);
                m.SetFloat("_UnderlayDilate", 0.35f);
                m.SetFloat("_UnderlaySoftness", 0.85f);
            });
        }

        public static Material Shadow(TMP_FontAsset font, bool strong = false)
        {
            if (font == null) return null;
            string key = $"TMP_Sombra{(strong ? "Fuerte" : "")}_{font.name.Replace(" ", "")}";
            return Effect(key, font, m =>
            {
                m.EnableKeyword("UNDERLAY_ON");
                m.SetColor("_UnderlayColor", new Color(0f, 0.02f, 0.06f, strong ? 0.85f : 0.6f));
                m.SetFloat("_UnderlayOffsetX", strong ? 0.25f : 0.45f);
                m.SetFloat("_UnderlayOffsetY", strong ? -0.25f : -0.45f);
                m.SetFloat("_UnderlayDilate", strong ? 0.55f : 0.1f);
                m.SetFloat("_UnderlaySoftness", strong ? 0.6f : 0.3f);
            });
        }

        static Material Effect(string key, TMP_FontAsset font, System.Action<Material> setup)
        {
            if (font == null) return null;
            if (effects.TryGetValue(key, out var m) && m != null) return m;
            m = Resources.Load<Material>("LabMaterials/Tinted/" + key);
            if (m == null)
            {
                m = new Material(font.material) { name = key };
                setup(m);
                // El constructor de escenas (editor) los guarda como asset junto a los materiales tenidos.
                if (!Application.isPlaying) LabFactory.PersistHook?.Invoke(m);
            }
            effects[key] = m;
            return m;
        }
    }
}

using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace LabQuimicaVR.EditorTools
{
    /// <summary>
    /// Crea las fuentes SDF de TextMeshPro (Resources/Fuentes) a partir de los .ttf de Assets/Laboratorio/Fuentes.
    /// Son atlas estaticos con todos los caracteres que usa el juego (codigo y datos): no hace falta el .ttf en el
    /// celular y no se generan letras mientras se juega. Lo que falte (flechas, simbolos) sale de LiberationSans.
    /// </summary>
    public static class LabFonts
    {
        const string SrcDir = "Assets/Laboratorio/Fuentes";
        const string OutDir = "Assets/Resources/Fuentes";
        const string Fallback = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
        const string TmpSettings = "Assets/TextMesh Pro/Resources/TMP Settings.asset";

        [MenuItem("Quimikaze/Crear fuentes", priority = 5)]
        public static void Build()
        {
            if (!AssetDatabase.IsValidFolder(OutDir)) AssetDatabase.CreateFolder("Assets/Resources", "Fuentes");
            // Los materiales de brillo y sombra apuntan al atlas viejo: se vuelven a crear al construir las escenas.
            foreach (var old in Directory.GetFiles(LabAssets.TintDir, "TMP_*.mat"))
                AssetDatabase.DeleteAsset(old.Replace('\\', '/'));

            string chars = CharactersUsed();
            var fallback = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Fallback);
            var bold = Create("AtkinsonHyperlegibleNext-Bold.ttf", LabType.BoldFont, chars, fallback);
            var body = Create("AtkinsonHyperlegibleNext-Medium.ttf", LabType.BodyFont, chars, fallback);
            var title = Create("Fredoka-SemiBold.ttf", LabType.TitleFont, chars, body, fallback);

            // <b> dentro de un parrafo usa la Atkinson Bold real (no una negrita falsa engordada).
            var so = new SerializedObject(body);
            var table = so.FindProperty("m_FontWeightTable");
            table.arraySize = Mathf.Max(table.arraySize, 10);
            table.GetArrayElementAtIndex(7).FindPropertyRelative("regularTypeface").objectReferenceValue = bold;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Fuente por defecto de cualquier texto nuevo.
            var settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(TmpSettings);
            if (settings != null)
            {
                var ss = new SerializedObject(settings);
                ss.FindProperty("m_defaultFontAsset").objectReferenceValue = body;
                ss.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(settings);
            }
            AssetDatabase.SaveAssets();
            LabAutoSync.WriteStatus($"OK: fuentes creadas ({chars.Length} caracteres)");
        }

        static TMP_FontAsset Create(string ttf, string name, string chars, params TMP_FontAsset[] fallbacks)
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>($"{SrcDir}/{ttf}");
            if (font == null) throw new FileNotFoundException(ttf);
            string path = $"{OutDir}/{name}.asset";
            AssetDatabase.DeleteAsset(path);

            // 72 pt con 10 px de margen: deja espacio para el halo y la sombra sin perder nitidez.
            TMP_FontAsset fa = null;
            string missing = "";
            foreach (var (w, h) in new[] { (1024, 1024), (2048, 1024), (2048, 2048) })
            {
                if (fa != null) Object.DestroyImmediate(fa);
                fa = TMP_FontAsset.CreateFontAsset(font, 72, 10, GlyphRenderMode.SDFAA, w, h, AtlasPopulationMode.Dynamic, false);
                fa.TryAddCharacters(chars, out missing);
                // Solo cuentan como faltantes los caracteres que la fuente tiene pero no cupieron en el atlas.
                FontEngine.LoadFontFace(font, 72);
                if (!missing.Any(c => FontEngine.TryGetGlyphIndex(c, out uint g) && g != 0)) break;
            }
            fa.name = name;
            fa.atlasPopulationMode = AtlasPopulationMode.Static;
            fa.fallbackFontAssetTable = fallbacks.Where(f => f != null).ToList();

            AssetDatabase.CreateAsset(fa, path);
            var atlas = fa.atlasTexture;
            atlas.name = name + " Atlas";
            AssetDatabase.AddObjectToAsset(atlas, fa);
            var mat = fa.material;
            mat.name = name + " Material";
            // Version movil del shader: mas liviana en celulares de gama baja, y tambien hace contorno y sombra.
            var mobile = Shader.Find("TextMeshPro/Mobile/Distance Field");
            if (mobile != null) mat.shader = mobile;
            AssetDatabase.AddObjectToAsset(mat, fa);
            EditorUtility.SetDirty(fa);
            if (missing.Length > 0) Debug.Log($"[LabFonts] {name}: sin glifo para '{missing}' (se usa la fuente de respaldo)");
            return fa;
        }

        /// <summary>Todos los caracteres que aparecen en los scripts y en los datos, mas el alfabeto basico.</summary>
        static string CharactersUsed()
        {
            var set = new HashSet<char>();
            for (char c = ' '; c <= '~'; c++) set.Add(c);
            foreach (char c in "áéíóúÁÉÍÓÚñÑüÜ¿¡°·×±²³–—“”‘’«»…•") set.Add(c);
            var files = Directory.GetFiles("Assets/Scripts", "*.cs", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles("Assets/Editor", "*.cs", SearchOption.AllDirectories))
                .Concat(Directory.GetFiles("Assets/Resources/Datos", "*.txt", SearchOption.AllDirectories));
            foreach (var f in files)
                foreach (char c in File.ReadAllText(f))
                    if (c >= ' ' && c != '﻿' && !char.IsSurrogate(c)) set.Add(c);
            return new string(set.OrderBy(c => c).ToArray());
        }
    }
}

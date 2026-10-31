using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace LabQuimicaVR
{
    /// <summary>Como se ve el elemento en la realidad: forma y material de la muestra 3D.</summary>
    public class SampleInfo
    {
        public int Z;
        public string Form = "desconocido";
        public Color Color = Color.gray;
        public float Metallic, Smoothness;
        public Color? Color2;
        public HashSet<string> Extra = new HashSet<string>();
        public string Looks = "", Nature = "", Landscape = "tunel", PhotoCaption = "";

        public bool Has(string extra) => Extra.Contains(extra);
    }

    /// <summary>Descubridor, laboratorio o imagen historica relacionada con un elemento.</summary>
    public class PersonInfo
    {
        public string Key, Name, Description, Image, Author, License;
        public int Born, Died;
        public char Kind = 'P'; // P persona, H imagen historica, I institucion

        public bool IsPerson => Kind == 'P';
        public string Years => Born == 0 ? "" : Died == 0 ? $"nació en {Born}" : $"{Born} – {Died}";
        public Texture2D LoadImage() => string.IsNullOrEmpty(Image) ? null : Resources.Load<Texture2D>("Fotos/Personas/" + Image);
    }

    public class LandscapeInfo
    {
        public string Key, File, Title, Author, License;
        public Texture2D Load() => Resources.Load<Texture2D>("Paisajes/" + File);
    }

    /// <summary>
    /// Datos extra de la version 1.1: muestras reales, fotos, descubridores con retrato y paisajes.
    /// Todo se lee de archivos de texto en Resources/Datos.
    /// </summary>
    public static class ElementExtras
    {
        static Dictionary<int, SampleInfo> samples;
        static Dictionary<string, PersonInfo> people;
        static Dictionary<int, string[]> discoverers;
        static Dictionary<string, LandscapeInfo> landscapes;
        static Dictionary<int, (string author, string license)> photoCredits;

        public static void Reload() { samples = null; people = null; discoverers = null; landscapes = null; photoCredits = null; }

        static IEnumerable<string[]> Rows(string resource)
        {
            var t = Resources.Load<TextAsset>(resource);
            if (t == null) { Debug.LogWarning("Falta Resources/" + resource); yield break; }
            foreach (var raw in t.text.Split('\n'))
            {
                var line = raw.Trim('\r', ' ', '﻿');
                if (line.Length == 0 || line.StartsWith("#")) continue;
                yield return line.Split('|').Select(s => s.Trim()).ToArray();
            }
        }

        static Color Hex(string s, Color fallback) =>
            !string.IsNullOrEmpty(s) && s != "-" && ColorUtility.TryParseHtmlString("#" + s, out var c) ? c : fallback;

        static float F(string s) => float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : 0f;

        public static SampleInfo Sample(ElementInfo e)
        {
            if (samples == null)
            {
                samples = new Dictionary<int, SampleInfo>();
                foreach (var c in Rows("Datos/muestras"))
                {
                    if (c.Length < 11 || !int.TryParse(c[0], out var z)) continue;
                    var s = new SampleInfo
                    {
                        Z = z, Form = c[1], Color = Hex(c[2], Color.gray), Metallic = F(c[3]), Smoothness = F(c[4]),
                        Looks = c[7], Nature = c[8], Landscape = c[9], PhotoCaption = c[10] == "-" ? "" : c[10]
                    };
                    if (c[5] != "-") s.Color2 = Hex(c[5], Color.white);
                    if (c[6] != "-") foreach (var x in c[6].Split(',')) s.Extra.Add(x.Trim());
                    samples[z] = s;
                }
            }
            if (samples.TryGetValue(e.Z, out var info)) return info;
            // Superpesados: solo existieron unos pocos atomos.
            return samples[e.Z] = new SampleInfo
            {
                Z = e.Z, Form = "desconocido", Color = new Color(1f, 0.42f, 0.42f), Landscape = "tunel",
                Looks = "Solo se han creado unos pocos átomos, que duran fracciones de segundo: nadie lo ha visto a simple vista.",
                Nature = "No existe en la naturaleza: se crea en aceleradores de partículas, haciendo chocar átomos a enorme velocidad."
            };
        }

        public static Texture2D Photo(int z) => Resources.Load<Texture2D>($"Fotos/Elementos/{z:000}");

        public static string PhotoCredit(int z)
        {
            if (photoCredits == null)
            {
                photoCredits = new Dictionary<int, (string, string)>();
                foreach (var c in Rows("Datos/fotos_elementos"))
                    if (c.Length >= 3 && int.TryParse(c[0], out var zz)) photoCredits[zz] = (c[1], c[2]);
            }
            return photoCredits.TryGetValue(z, out var p) ? Credit(p.author, p.license) : "";
        }

        public static string Credit(string author, string license)
        {
            if (string.IsNullOrEmpty(author) && string.IsNullOrEmpty(license)) return "";
            if (string.IsNullOrEmpty(author)) return "Imagen: " + license;
            return $"Imagen: {author} · {license}";
        }

        public static List<PersonInfo> Discoverers(int z)
        {
            if (people == null)
            {
                people = new Dictionary<string, PersonInfo>();
                foreach (var c in Rows("Datos/personas"))
                {
                    if (c.Length < 9) continue;
                    people[c[0]] = new PersonInfo
                    {
                        Key = c[0], Name = c[1], Born = int.TryParse(c[2], out var b) ? b : 0, Died = int.TryParse(c[3], out var d) ? d : 0,
                        Description = c[4], Image = c[5], Kind = c[6].Length > 0 ? c[6][0] : 'P', Author = c[7], License = c[8]
                    };
                }
                discoverers = new Dictionary<int, string[]>();
                foreach (var c in Rows("Datos/descubridores"))
                    if (c.Length >= 2 && int.TryParse(c[0], out var zz))
                        discoverers[zz] = c[1].Split(new[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries);
            }
            var list = new List<PersonInfo>();
            if (discoverers.TryGetValue(z, out var keys))
                foreach (var k in keys)
                    if (people.TryGetValue(k, out var p)) list.Add(p);
            return list;
        }

        public static LandscapeInfo Landscape(string key)
        {
            if (landscapes == null)
            {
                landscapes = new Dictionary<string, LandscapeInfo>();
                foreach (var c in Rows("Datos/paisajes"))
                    if (c.Length >= 5) landscapes[c[0]] = new LandscapeInfo { Key = c[0], File = c[1], Title = c[2], Author = c[3], License = c[4] };
            }
            if (key != null && landscapes.TryGetValue(key, out var l)) return l;
            return landscapes.TryGetValue("tunel", out var t) ? t : null;
        }

        public static IEnumerable<LandscapeInfo> AllLandscapes { get { Landscape(null); return landscapes.Values; } }
    }
}

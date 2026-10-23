using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace LabQuimicaVR
{
    public enum ElementCategory
    {
        AlkaliMetal, AlkalineEarth, Transition, PostTransition, Metalloid,
        Nonmetal, Halogen, NobleGas, Lanthanide, Actinide
    }

    public class TimelineEvent
    {
        public string Year;
        public string Text;
    }

    public class ElementInfo
    {
        public int Z;
        public string Symbol, Name, MassText;
        public float Mass;
        public ElementCategory Category;
        public char State;
        public string Electronegativity, Melting, Boiling, Density;
        public int[] Valences = Array.Empty<int>();
        public bool ValencesUnknown;
        public string Discoverer, Year;
        public Color CpkColor = Color.white;

        public string NameOrigin = "", History = "", Value = "", Curiosity = "";
        public List<TimelineEvent> Timeline = new List<TimelineEvent>();

        public int Period, Group;      // Group 0 = bloque f (lantanidos / actinidos)
        public char Block;
        public int TableRow, TableCol; // posicion en la tabla (filas 9 y 10 para el bloque f)
        public List<Orbital> Orbitals; // en orden de llenado (regla de Moeller)
        public int[] Shells;           // electrones por nivel K, L, M...

        public int Neutrons => Mathf.Max(0, Mathf.RoundToInt(Mass) - Z);
        public bool IsRadioactive => Z == 43 || Z == 61 || Z >= 84;
        public bool IsSynthetic => Z == 43 || Z == 61 || Z == 85 || Z >= 93;
        public bool IsSuperheavy => Z >= 104;
        public bool IsMetal => Category == ElementCategory.AlkaliMetal || Category == ElementCategory.AlkalineEarth ||
                               Category == ElementCategory.Transition || Category == ElementCategory.PostTransition ||
                               Category == ElementCategory.Lanthanide || Category == ElementCategory.Actinide;

        /// <summary>Articulo en espanol: "la plata", "el oro".</summary>
        public string Article => Symbol == "Ag" ? "la" : "el";
        public string Del => Symbol == "Ag" ? "de la" : "del";

        public int MainPositiveValence => Valences.FirstOrDefault(v => v > 0);
        public int MainNegativeValence => Valences.FirstOrDefault(v => v < 0);
        public string StateName => State switch
        {
            'S' => "Sólido",
            'L' => "Líquido",
            'G' => "Gas",
            _ => "Desconocido"
        };
    }

    public readonly struct Orbital
    {
        public readonly int N, L, Electrons;
        public Orbital(int n, int l, int e) { N = n; L = l; Electrons = e; }
        public char Letter => "spdf"[L];
        public int Capacity => 2 * (2 * L + 1);
    }

    public static class ElementDatabase
    {
        static ElementInfo[] elements;

        public static IReadOnlyList<ElementInfo> All { get { EnsureLoaded(); return elements; } }
        public static int Count => All.Count;

        public static ElementInfo Get(int z)
        {
            EnsureLoaded();
            z = Mathf.Clamp(z, 1, elements.Length);
            return elements[z - 1];
        }

        public static ElementInfo BySymbol(string symbol)
        {
            EnsureLoaded();
            return elements.FirstOrDefault(e => e.Symbol == symbol);
        }

        public static void Reload() { elements = null; EnsureLoaded(); }

        static void EnsureLoaded()
        {
            if (elements != null) return;
            var list = new List<ElementInfo>();
            var baseText = Resources.Load<TextAsset>("Datos/elementos_base");
            if (baseText == null)
            {
                Debug.LogError("No se encontró Resources/Datos/elementos_base.txt");
                elements = Array.Empty<ElementInfo>();
                return;
            }
            foreach (var cols in Rows(baseText.text))
            {
                if (cols.Length < 14) continue;
                var e = new ElementInfo
                {
                    Z = int.Parse(cols[0]),
                    Symbol = cols[1],
                    Name = cols[2],
                    MassText = cols[3],
                    Mass = ParseMass(cols[3]),
                    Category = ParseCategory(cols[4]),
                    State = cols[5].Length > 0 ? cols[5][0] : '?',
                    Electronegativity = cols[6],
                    Melting = cols[7],
                    Boiling = cols[8],
                    Density = cols[9],
                    Discoverer = cols[11],
                    Year = cols[12],
                };
                ParseValences(cols[10], e);
                if (ColorUtility.TryParseHtmlString("#" + cols[13], out var c)) e.CpkColor = c;
                list.Add(e);
            }
            elements = list.OrderBy(e => e.Z).ToArray();

            foreach (var file in new[] { "Datos/elementos_historia_1", "Datos/elementos_historia_2" })
            {
                var t = Resources.Load<TextAsset>(file);
                if (t == null) continue;
                foreach (var cols in Rows(t.text))
                {
                    if (cols.Length < 6 || !int.TryParse(cols[0], out var z) || z < 1 || z > elements.Length) continue;
                    var e = elements[z - 1];
                    e.NameOrigin = cols[1];
                    e.History = cols[2];
                    e.Timeline = cols[3].Split(';')
                        .Select(s => s.Split(new[] { '=' }, 2))
                        .Where(p => p.Length == 2)
                        .Select(p => new TimelineEvent { Year = p[0].Trim(), Text = p[1].Trim() })
                        .ToList();
                    e.Value = cols[4];
                    e.Curiosity = cols[5];
                }
            }

            foreach (var e in elements)
            {
                PeriodicLayout.Assign(e);
                e.Orbitals = ElectronConfiguration.Compute(e.Z);
                e.Shells = ElectronConfiguration.Shells(e.Orbitals);
            }
        }

        static IEnumerable<string[]> Rows(string text)
        {
            foreach (var raw in text.Split('\n'))
            {
                var line = raw.Trim('\r', ' ', '﻿');
                if (line.Length == 0 || line.StartsWith("#")) continue;
                yield return line.Split('|').Select(s => s.Trim()).ToArray();
            }
        }

        static float ParseMass(string s)
        {
            s = s.Trim('[', ']');
            float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var m);
            return m;
        }

        static void ParseValences(string s, ElementInfo e)
        {
            if (s == "?" || string.IsNullOrEmpty(s)) { e.ValencesUnknown = true; return; }
            e.Valences = s.Split(',')
                .Select(v => int.TryParse(v.Trim(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var n) ? n : 0)
                .ToArray();
        }

        static ElementCategory ParseCategory(string code) => code switch
        {
            "AM" => ElementCategory.AlkaliMetal,
            "AE" => ElementCategory.AlkalineEarth,
            "TM" => ElementCategory.Transition,
            "PT" => ElementCategory.PostTransition,
            "ML" => ElementCategory.Metalloid,
            "NM" => ElementCategory.Nonmetal,
            "HA" => ElementCategory.Halogen,
            "NG" => ElementCategory.NobleGas,
            "LA" => ElementCategory.Lanthanide,
            "AC" => ElementCategory.Actinide,
            _ => ElementCategory.Nonmetal
        };
    }

    /// <summary>Posicion de cada elemento en la tabla, grupos, periodos y bloques.</summary>
    public static class PeriodicLayout
    {
        static readonly int[] PeriodStart = { 1, 3, 11, 19, 37, 55, 87, 119 };

        public static void Assign(ElementInfo e)
        {
            int z = e.Z;
            int p = 1;
            while (p < 7 && z >= PeriodStart[p]) p++;
            e.Period = p;
            int off = z - PeriodStart[p - 1];

            int group;
            if (p == 1) group = z == 1 ? 1 : 18;
            else if (p <= 3) group = off < 2 ? off + 1 : off + 11;
            else if (p <= 5) group = off + 1;
            else if (off < 2) group = off + 1;
            else if (off <= 16) group = 0;
            else group = off - 13;
            e.Group = group;

            if (group == 0)
            {
                e.Block = 'f';
                e.TableRow = p == 6 ? 9 : 10;
                e.TableCol = 3 + off - 2;
            }
            else
            {
                e.Block = group <= 2 || z == 2 ? 's' : group >= 13 ? 'p' : 'd';
                e.TableRow = p;
                e.TableCol = group;
            }
        }

        static readonly string[] Roman =
        {
            "", "IA", "IIA", "IIIB", "IVB", "VB", "VIB", "VIIB", "VIIIB", "VIIIB", "VIIIB",
            "IB", "IIB", "IIIA", "IVA", "VA", "VIA", "VIIA", "VIIIA"
        };

        static readonly string[] GroupNames =
        {
            "",
            "Metales alcalinos",
            "Metales alcalinotérreos",
            "Familia del escandio",
            "Familia del titanio",
            "Familia del vanadio",
            "Familia del cromo",
            "Familia del manganeso",
            "Familia del hierro",
            "Familia del cobalto",
            "Familia del níquel",
            "Metales de acuñación (familia del cobre)",
            "Familia del zinc",
            "Térreos o boroides",
            "Carbonoides",
            "Nitrogenoides",
            "Anfígenos o calcógenos",
            "Halógenos",
            "Gases nobles"
        };

        public static string RomanGroup(int group) => group >= 1 && group <= 18 ? Roman[group] : "IIIB";
        public static string GroupName(int group) => group >= 1 && group <= 18 ? GroupNames[group] : "";

        public static string GroupLabel(ElementInfo e)
        {
            if (e.Group == 0) return "3 (IIIB) · bloque f";
            return $"{e.Group} ({RomanGroup(e.Group)})";
        }

        public static string GroupFamilyName(ElementInfo e)
        {
            if (e.Group == 0) return e.Period == 6 ? "Lantánidos (tierras raras)" : "Actínidos";
            if (e.Z == 1) return "Grupo 1, pero el hidrógeno no es un metal alcalino";
            return GroupName(e.Group);
        }
    }

    public static class CategoryInfo
    {
        public static readonly ElementCategory[] AllCategories =
            (ElementCategory[])Enum.GetValues(typeof(ElementCategory));

        public static string Name(ElementCategory c) => c switch
        {
            ElementCategory.AlkaliMetal => "Metal alcalino",
            ElementCategory.AlkalineEarth => "Metal alcalinotérreo",
            ElementCategory.Transition => "Metal de transición",
            ElementCategory.PostTransition => "Metal del bloque p",
            ElementCategory.Metalloid => "Metaloide",
            ElementCategory.Nonmetal => "No metal",
            ElementCategory.Halogen => "Halógeno",
            ElementCategory.NobleGas => "Gas noble",
            ElementCategory.Lanthanide => "Lantánido",
            ElementCategory.Actinide => "Actínido",
            _ => "?"
        };

        public static string Code(ElementCategory c) => c switch
        {
            ElementCategory.AlkaliMetal => "AM",
            ElementCategory.AlkalineEarth => "AE",
            ElementCategory.Transition => "TM",
            ElementCategory.PostTransition => "PT",
            ElementCategory.Metalloid => "ML",
            ElementCategory.Nonmetal => "NM",
            ElementCategory.Halogen => "HA",
            ElementCategory.NobleGas => "NG",
            ElementCategory.Lanthanide => "LA",
            ElementCategory.Actinide => "AC",
            _ => "NM"
        };

        public static Color Color(ElementCategory c) => Hex(c switch
        {
            ElementCategory.AlkaliMetal => "FF8787",
            ElementCategory.AlkalineEarth => "FFC078",
            ElementCategory.Transition => "FFE066",
            ElementCategory.PostTransition => "A9E34B",
            ElementCategory.Metalloid => "38D9A9",
            ElementCategory.Nonmetal => "74C0FC",
            ElementCategory.Halogen => "748FFC",
            ElementCategory.NobleGas => "B197FC",
            ElementCategory.Lanthanide => "F783AC",
            ElementCategory.Actinide => "E599F7",
            _ => "FFFFFF"
        });

        public static string Description(ElementCategory c) => c switch
        {
            ElementCategory.AlkaliMetal => "Metales blandos y muy reactivos. Reaccionan con fuerza con el agua formando hidróxidos y liberando hidrógeno; por eso se guardan en aceite.",
            ElementCategory.AlkalineEarth => "Metales reactivos, aunque menos que los alcalinos. Forman óxidos básicos y muchos están en rocas, huesos y conchas.",
            ElementCategory.Transition => "Metales duros, brillantes y buenos conductores. Suelen tener varias valencias y forman compuestos de colores.",
            ElementCategory.PostTransition => "Metales más blandos y con puntos de fusión más bajos que los de transición.",
            ElementCategory.Metalloid => "Tienen propiedades intermedias entre metales y no metales. Muchos son semiconductores, clave en la electrónica.",
            ElementCategory.Nonmetal => "No conducen bien la electricidad ni el calor. Forman moléculas y son la base de la vida.",
            ElementCategory.Halogen => "No metales muy reactivos que forman sales al unirse con metales. Su nombre significa \"formador de sales\".",
            ElementCategory.NobleGas => "Tienen su último nivel lleno de electrones, por eso casi nunca reaccionan.",
            ElementCategory.Lanthanide => "Las \"tierras raras\": metales parecidos entre sí, usados en imanes, pantallas y láseres.",
            ElementCategory.Actinide => "Metales radiactivos. Algunos, como el uranio, se usan en la energía nuclear.",
            _ => ""
        };

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            return c;
        }
    }
}

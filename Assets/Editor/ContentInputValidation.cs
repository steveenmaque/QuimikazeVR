using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace LabQuimicaVR.EditorTools
{
    /// <summary>Invariantes de contenido y controles; no sustituye una revisión científica de cada afirmación.</summary>
    public static class ContentInputValidation
    {
        static int checks;

        [MenuItem("Quimikaze/Validar contenido y controles", priority = 45)]
        public static void RunMenu() => Debug.Log("Contenido/controles: " + Run() + " comprobaciones correctas.");

        public static int Run()
        {
            checks = 0;
            CheckInput();
            CheckRows("elementos_base", 118, 14);
            CheckRows("elementos_historia_1", 56, 6);
            CheckRows("elementos_historia_2", 62, 6);
            CheckRows("descubridores", 118, 2);
            ElementDatabase.Reload();
            ElementExtras.Reload();
            Require(ElementDatabase.Count == 118, "Faltan elementos");
            var positions = new HashSet<string>();
            var symbols = new HashSet<string>();
            foreach (var e in ElementDatabase.All)
            {
                Require(e.Z >= 1 && e.Z <= 118 && ReferenceEquals(ElementDatabase.Get(e.Z), e), "Índice Z inconsistente");
                Require(symbols.Add(e.Symbol) && Regex.IsMatch(e.Symbol, "^[A-Z][a-z]?$"), "Símbolo duplicado o inválido: " + e.Z);
                Require(!string.IsNullOrWhiteSpace(e.Name) && e.Mass > 0 && !float.IsInfinity(e.Mass), "Identidad/masa: " + e.Z);
                Require(Enum.IsDefined(typeof(ElementCategory), e.Category), "Categoría: " + e.Z);
                Require(e.Valences.All(v => v >= -8 && v <= 8), "Valencias fuera del modelo: " + e.Z);
                Require(e.Orbitals.Sum(o => o.Electrons) == e.Z, "Electrones no conservados: " + e.Z);
                Require(e.Orbitals.All(o => o.N >= 1 && o.N <= 7 && o.L >= 0 && o.L < o.N && o.Electrons > 0 && o.Electrons <= o.Capacity), "Orbital inválido: " + e.Z);
                Require(e.Shells.Sum() == e.Z && e.Shells.Length <= 7, "Capas no conservadas: " + e.Z);
                Require(e.Period >= 1 && e.Period <= 7 && e.Group >= 0 && e.Group <= 18, "Posición fuera de tabla: " + e.Z);
                Require(positions.Add(e.TableRow + ":" + e.TableCol), "Casillas superpuestas: " + e.Z);
                Require(!string.IsNullOrWhiteSpace(e.History) && !string.IsNullOrWhiteSpace(e.NameOrigin) && !string.IsNullOrWhiteSpace(e.Value) && !string.IsNullOrWhiteSpace(e.Curiosity), "Viñeta incompleta: " + e.Z);
                Require(e.Timeline.Count > 0 && e.Timeline.All(t => !string.IsNullOrWhiteSpace(t.Year) && !string.IsNullOrWhiteSpace(t.Text)), "Cronología vacía: " + e.Z);
                Require(!string.IsNullOrEmpty(ElectronConfiguration.Full(e)) && !string.IsNullOrEmpty(ElectronConfiguration.Kernel(e)), "Configuración vacía: " + e.Z);
                var sample = ElementExtras.Sample(e);
                Require(sample.Z == e.Z && !string.IsNullOrWhiteSpace(sample.Looks) && !string.IsNullOrWhiteSpace(sample.Nature), "Muestra sin descripción: " + e.Z);
                var landscape = ElementExtras.Landscape(sample.Landscape);
                Require(landscape != null && landscape.Load() != null, "Paisaje ausente: " + e.Z);
                var people = ElementExtras.Discoverers(e.Z);
                Require(people.Count > 0 && people.All(p => !string.IsNullOrWhiteSpace(p.Name)), "Descubridor sin ficha: " + e.Z);
                Require(people.Where(p => !string.IsNullOrEmpty(p.Image)).All(p => p.LoadImage() != null), "Retrato declarado ausente: " + e.Z);
                if (e.Z >= 104) Require(e.State == '?', "Estado macroscópico superpesado presentado como conocido: " + e.Z);
            }
            Require(ElementDatabase.Get(83).IsRadioactive, "Bi-209 tiene decaimiento alfa observado");
            Require(!ElementDatabase.Get(85).IsSynthetic, "El astato tiene presencia natural");
            CheckReactions();
            return checks;
        }

        static void CheckInput()
        {
            foreach (bool inverted in new[] { false, true })
            {
                float sign = inverted ? -1f : 1f;
                Require(LabInput.NormalizeRightStickVertical(-1f, true, inverted) == -sign, "Arriba Android");
                Require(LabInput.NormalizeRightStickVertical(1f, false, inverted) == -sign, "Arriba Windows");
                Require(LabInput.NormalizeRightStickVertical(1f, true, inverted) == sign, "Abajo Android");
                Require(LabInput.NormalizeRightStickVertical(-1f, false, inverted) == sign, "Abajo Windows");
                Require(LabInput.NormalizeRightStickVertical(0.1f, true, inverted) == 0f, "Zona muerta Android");
                Require(LabInput.NormalizeRightStickVertical(-0.1f, false, inverted) == 0f, "Zona muerta Windows");
            }
            Require(LabInput.NormalizeRightStickVertical(float.NaN, true, false) == 0f, "NaN debe quedar neutro");
            Require(LabInput.NormalizeRightStickVertical(float.PositiveInfinity, false, false) == 0f, "Infinito debe quedar neutro");
            Require(LabInput.NormalizeRightStickVertical(2f, true, false) == 1f, "Eje limitado");
            Require(LabInput.IntegrateLookPitch(0, -1, 70, 0.02f) < 0, "Negativo debe inclinar arriba en Unity");
            Require(LabInput.IntegrateLookPitch(0, 1, 70, 0.02f) > 0, "Positivo debe inclinar abajo en Unity");
            Require(LabInput.IntegrateLookPitch(-65, -1, 70, 0.02f) == -65, "Límite superior de mirada");
            Require(LabInput.IntegrateLookPitch(65, 1, 70, 0.02f) == 65, "Límite inferior de mirada");
            Require(LabInput.IntegrateLookPitch(0, 1, 70, 10f) <= 7.001f, "Un frame lento no debe voltear la mirada");
        }

        static void CheckRows(string name, int expected, int columns)
        {
            var resource = Resources.Load<TextAsset>("Datos/" + name);
            Require(resource != null, "Recurso ausente: " + name);
            var rows = resource.text.Split('\n').Select(l => l.Trim('\r', ' ', '\uFEFF')).Where(l => l.Length > 0 && !l.StartsWith("#")).Select(l => l.Split('|')).ToArray();
            Require(rows.Length == expected && rows.All(r => r.Length == columns), "Filas/columnas de " + name);
            Require(rows.Select(r => r[0]).Distinct().Count() == expected, "Claves duplicadas en " + name);
        }

        static void CheckReactions()
        {
            int documented = 0;
            foreach (var a in ElementDatabase.All)
                foreach (var b in ElementDatabase.All.Where(b => b.Z >= a.Z))
                {
                    var result = ReactionDatabase.React(a, b);
                    var reverse = ReactionDatabase.React(b, a);
                    Require(result != null && !string.IsNullOrEmpty(result.Description) && !string.IsNullOrEmpty(result.ProductName), "Resultado vacío: " + a.Symbol + "/" + b.Symbol);
                    Require(result.Predicted == reverse.Predicted && result.Formula == reverse.Formula && result.Effect == reverse.Effect, "Pareja no simétrica: " + a.Symbol + "/" + b.Symbol);
                    if (result.Predicted)
                        Require(result.Effect == ReactionEffect.None, "Efecto experimental inferido sin datos: " + a.Symbol + "/" + b.Symbol);
                    else documented++;
                    if (!string.IsNullOrEmpty(result.Formula))
                    {
                        var sides = result.Equation.Split(new[] { "->" }, StringSplitOptions.None);
                        Require(sides.Length == 2, "Ecuación sin dos miembros: " + result.Equation);
                        var left = CountAtoms(sides[0]);
                        var right = CountAtoms(sides[1]);
                        Require(left.Count == right.Count && left.All(p => right.TryGetValue(p.Key, out int n) && n == p.Value), "Ecuación no balanceada: " + result.Equation);
                    }
                }
            Require(documented > 40, "Catálogo documentado reducido inesperadamente");
            Require(ReactionDatabase.React(ElementDatabase.Get(54), ElementDatabase.Get(9)).Formula == "XeF2", "Excepción de xenón documentada perdida");
        }

        static Dictionary<string, int> CountAtoms(string side)
        {
            var counts = new Dictionary<string, int>();
            foreach (string term in side.Split('+'))
            {
                var match = Regex.Match(term.Trim(), @"^(\d*)([A-Z][A-Za-z0-9]*)$");
                Require(match.Success, "Término de ecuación inválido: " + term);
                int coefficient = match.Groups[1].Length == 0 ? 1 : int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                foreach (var atom in ReactionDatabase.ParseFormula(match.Groups[2].Value))
                {
                    Require(ElementDatabase.BySymbol(atom.symbol) != null, "Símbolo de fórmula desconocido: " + atom.symbol);
                    counts.TryGetValue(atom.symbol, out int n);
                    counts[atom.symbol] = n + coefficient * atom.count;
                }
            }
            return counts;
        }

        static void Require(bool condition, string message)
        {
            checks++;
            if (!condition) throw new InvalidOperationException("Contenido/controles: " + message);
        }
    }
}

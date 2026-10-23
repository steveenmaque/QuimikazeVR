using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LabQuimicaVR
{
    /// <summary>
    /// Configuracion electronica por la regla de Moeller (diagonales), con las excepciones
    /// reales mas conocidas (Cr, Cu, Pd, Au...).
    /// </summary>
    public static class ElectronConfiguration
    {
        // Orden de llenado: 1s 2s 2p 3s 3p 4s 3d 4p 5s 4d 5p 6s 4f 5d 6p 7s 5f 6d 7p
        static readonly (int n, int l)[] FillOrder = BuildOrder();

        static (int n, int l)[] BuildOrder()
        {
            var list = new List<(int n, int l)>();
            for (int n = 1; n <= 7; n++)
                for (int l = 0; l < n && l <= 3; l++)
                    list.Add((n, l));
            return list.OrderBy(o => o.n + o.l).ThenBy(o => o.n).ToArray();
        }

        // Excepciones: (orbital que pierde, orbital que gana, cantidad)
        static readonly Dictionary<int, (string from, string to, int count)[]> Exceptions = new Dictionary<int, (string, string, int)[]>
        {
            { 24, new[] { ("4s", "3d", 1) } },
            { 29, new[] { ("4s", "3d", 1) } },
            { 41, new[] { ("5s", "4d", 1) } },
            { 42, new[] { ("5s", "4d", 1) } },
            { 44, new[] { ("5s", "4d", 1) } },
            { 45, new[] { ("5s", "4d", 1) } },
            { 46, new[] { ("5s", "4d", 2) } },
            { 47, new[] { ("5s", "4d", 1) } },
            { 57, new[] { ("4f", "5d", 1) } },
            { 58, new[] { ("4f", "5d", 1) } },
            { 64, new[] { ("4f", "5d", 1) } },
            { 78, new[] { ("6s", "5d", 1) } },
            { 79, new[] { ("6s", "5d", 1) } },
            { 89, new[] { ("5f", "6d", 1) } },
            { 90, new[] { ("5f", "6d", 2) } },
            { 91, new[] { ("5f", "6d", 1) } },
            { 92, new[] { ("5f", "6d", 1) } },
            { 93, new[] { ("5f", "6d", 1) } },
            { 96, new[] { ("5f", "6d", 1) } },
            { 103, new[] { ("6d", "7p", 1) } },
        };

        static readonly int[] NobleGases = { 2, 10, 18, 36, 54, 86, 118 };
        static readonly string[] NobleSymbols = { "He", "Ne", "Ar", "Kr", "Xe", "Rn", "Og" };
        public static readonly string ShellLetters = "KLMNOPQ";

        public static bool IsException(int z) => Exceptions.ContainsKey(z);

        public static List<Orbital> Compute(int z)
        {
            var occ = new Dictionary<(int, int), int>();
            int left = z;
            foreach (var o in FillOrder)
            {
                if (left <= 0) break;
                int cap = 2 * (2 * o.l + 1);
                int e = left < cap ? left : cap;
                occ[o] = e;
                left -= e;
            }
            if (Exceptions.TryGetValue(z, out var ex))
            {
                foreach (var (from, to, count) in ex)
                {
                    var f = Parse(from);
                    var t = Parse(to);
                    occ[f] = (occ.TryGetValue(f, out var a) ? a : 0) - count;
                    occ[t] = (occ.TryGetValue(t, out var b) ? b : 0) + count;
                }
            }
            return FillOrder
                .Where(o => occ.TryGetValue(o, out var e) && e > 0)
                .Select(o => new Orbital(o.n, o.l, occ[o]))
                .ToList();
        }

        static (int, int) Parse(string s) => (s[0] - '0', "spdf".IndexOf(s[1]));

        public static int[] Shells(List<Orbital> orbitals)
        {
            int max = orbitals.Count == 0 ? 0 : orbitals.Max(o => o.N);
            var shells = new int[max];
            foreach (var o in orbitals) shells[o.N - 1] += o.Electrons;
            return shells;
        }

        /// <summary>Configuracion completa en orden de llenado, con superindices TMP.</summary>
        public static string Full(ElementInfo e) => Format(e.Orbitals);

        /// <summary>Configuracion abreviada con el gas noble anterior (kernel).</summary>
        public static string Kernel(ElementInfo e)
        {
            int idx = -1;
            for (int i = 0; i < NobleGases.Length; i++) if (NobleGases[i] < e.Z) idx = i;
            if (idx < 0) return Format(e.Orbitals);
            int core = NobleGases[idx];
            var coreOrbitals = Compute(core);
            var rest = new List<Orbital>();
            foreach (var o in e.Orbitals)
            {
                var c = coreOrbitals.FirstOrDefault(k => k.N == o.N && k.L == o.L);
                int extra = o.Electrons - c.Electrons;
                if (extra > 0) rest.Add(new Orbital(o.N, o.L, extra));
            }
            return $"[{NobleSymbols[idx]}] " + Format(rest);
        }

        public static string Format(IEnumerable<Orbital> orbitals)
        {
            var sb = new StringBuilder();
            foreach (var o in orbitals)
            {
                if (sb.Length > 0) sb.Append(' ');
                sb.Append(o.N).Append(o.Letter).Append("<sup>").Append(o.Electrons).Append("</sup>");
            }
            return sb.ToString();
        }

        public static string ShellsText(ElementInfo e)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < e.Shells.Length; i++)
            {
                if (i > 0) sb.Append("  ");
                sb.Append(ShellLetters[i]).Append(": ").Append(e.Shells[i]);
            }
            return sb.ToString();
        }

        public static int OuterElectrons(ElementInfo e) => e.Shells.Length == 0 ? 0 : e.Shells[e.Shells.Length - 1];
    }
}

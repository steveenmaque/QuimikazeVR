using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace LabQuimicaVR
{
    public enum ReactionEffect { Explosion, Fire, BrightLight, Smoke, Glow, Slow, Alloy, None }

    public class ReactionResult
    {
        public string Equation;     // notacion compacta: "2H2 + O2 -> 2H2O"
        public string ProductName;
        public string Formula;      // producto principal, ej. "H2O"; vacio para mezclas
        public string Description;
        public ReactionEffect Effect;
        public Color EffectColor = Color.white;
        public bool Predicted;      // true si se dedujo con las valencias (no es una reaccion de la lista)
    }

    /// <summary>
    /// Reacciones comunes entre elementos. Las mas conocidas estan escritas a mano; para el resto
    /// se predice el compuesto binario usando valencias y electronegatividad.
    /// </summary>
    public static class ReactionDatabase
    {
        class Entry
        {
            public string A, B, Eq, Name, Formula, Desc, Color;
            public ReactionEffect Fx;
        }

        static readonly Entry[] Curated =
        {
            E("H","O","2H2 + O2 -> 2H2O","Agua","H2O","El hidrógeno arde con el oxígeno con una pequeña explosión y forma agua. ¡Así funcionan los motores de algunos cohetes!",ReactionEffect.Explosion,"9AD8FF"),
            E("H","Cl","H2 + Cl2 -> 2HCl","Cloruro de hidrógeno","HCl","Con luz reaccionan de golpe. Disuelto en agua forma el ácido clorhídrico, el mismo ácido que tienes en el estómago.",ReactionEffect.Explosion,"D9FF7A"),
            E("H","N","3H2 + N2 -> 2NH3","Amoníaco","NH3","Es la reacción de Haber: con presión, calor y un catalizador se fabrica amoníaco para los fertilizantes que alimentan al mundo.",ReactionEffect.Slow,"CFE8FF"),
            E("H","C","C + 2H2 -> CH4","Metano","CH4","El carbono y el hidrógeno forman metano, el gas natural que se usa en las cocinas.",ReactionEffect.Slow,"BFEFFF"),
            E("H","F","H2 + F2 -> 2HF","Fluoruro de hidrógeno","HF","Reaccionan de forma explosiva incluso en la oscuridad y a temperaturas muy bajas.",ReactionEffect.Explosion,"F2F2A0"),
            E("H","S","H2 + S -> H2S","Sulfuro de hidrógeno","H2S","Forma un gas con olor a huevo podrido. Lo emiten los volcanes y las aguas termales.",ReactionEffect.Smoke,"FFF59D"),
            E("H","Br","H2 + Br2 -> 2HBr","Bromuro de hidrógeno","HBr","Al calentarse reaccionan y forman un gas que en agua es un ácido fuerte.",ReactionEffect.Fire,"FFB199"),
            E("H","I","H2 + I2 -> 2HI","Yoduro de hidrógeno","HI","Es una reacción reversible: el producto también se vuelve a separar en hidrógeno y yodo.",ReactionEffect.Slow,"C9A3FF"),
            E("Na","H","2Na + H2 -> 2NaH","Hidruro de sodio","NaH","El sodio caliente absorbe hidrógeno y forma un sólido blanco muy reactivo.",ReactionEffect.Slow,"FFFFFF"),
            E("Ca","H","Ca + H2 -> CaH2","Hidruro de calcio","CaH2","Se usa para obtener hidrógeno en lugares alejados: al mojarse libera ese gas.",ReactionEffect.Slow,"FFFFFF"),
            E("Na","Cl","2Na + Cl2 -> 2NaCl","Cloruro de sodio (sal de mesa)","NaCl","Un metal blando y un gas venenoso reaccionan con una llama amarilla brillante... ¡y forman la sal que comes!",ReactionEffect.Fire,"FFD54F"),
            E("Na","O","4Na + O2 -> 2Na2O","Óxido de sodio","Na2O","El sodio pierde su brillo en segundos al tocar el aire porque se cubre de óxido.",ReactionEffect.Fire,"FFD54F"),
            E("Na","F","2Na + F2 -> 2NaF","Fluoruro de sodio","NaF","Reacción violenta. El fluoruro de sodio se usa en pastas dentales para proteger los dientes.",ReactionEffect.Explosion,"FFE082"),
            E("Na","K","Na + K -> NaK","Aleación sodio-potasio","","Al mezclarlos forman una aleación que es líquida a temperatura ambiente. Se usó para enfriar reactores.",ReactionEffect.Alloy,"E0E0E0"),
            E("K","Cl","2K + Cl2 -> 2KCl","Cloruro de potasio","KCl","Reacción con llama violeta. El cloruro de potasio se usa como fertilizante y sal dietética.",ReactionEffect.Fire,"CE93D8"),
            E("K","O","K + O2 -> KO2","Superóxido de potasio","KO2","El potasio arde con llama violeta. Este compuesto se usa en equipos de rescate porque libera oxígeno.",ReactionEffect.Fire,"CE93D8"),
            E("Li","O","4Li + O2 -> 2Li2O","Óxido de litio","Li2O","El litio arde con una hermosa llama roja.",ReactionEffect.Fire,"FF5252"),
            E("Li","N","6Li + N2 -> 2Li3N","Nitruro de litio","Li3N","El litio es el único metal alcalino que reacciona con el nitrógeno del aire a temperatura ambiente.",ReactionEffect.Slow,"FF8A80"),
            E("Mg","O","2Mg + O2 -> 2MgO","Óxido de magnesio","MgO","El magnesio arde con una luz blanca intensísima. ¡No se debe mirar directamente! Se usaba en los flashes antiguos.",ReactionEffect.BrightLight,"FFFFFF"),
            E("Mg","N","3Mg + N2 -> Mg3N2","Nitruro de magnesio","Mg3N2","Cuando el magnesio arde en el aire, una parte se une al nitrógeno.",ReactionEffect.BrightLight,"FFFDE7"),
            E("Mg","Cl","Mg + Cl2 -> MgCl2","Cloruro de magnesio","MgCl2","Se usa para obtener magnesio metálico del agua de mar.",ReactionEffect.Fire,"FFFFFF"),
            E("Ca","O","2Ca + O2 -> 2CaO","Óxido de calcio (cal viva)","CaO","La cal viva se usa en la construcción. Al mezclarla con agua se calienta mucho.",ReactionEffect.Fire,"FF7043"),
            E("Ca","F","Ca + F2 -> CaF2","Fluoruro de calcio (fluorita)","CaF2","Forma el mineral fluorita, que brilla bajo luz ultravioleta.",ReactionEffect.Fire,"B39DDB"),
            E("Ca","Cl","Ca + Cl2 -> CaCl2","Cloruro de calcio","CaCl2","Se usa para derretir el hielo de las carreteras y secar el aire.",ReactionEffect.Fire,"FF8A65"),
            E("Ba","O","2Ba + O2 -> 2BaO","Óxido de bario","BaO","El bario arde con llama verde, por eso se usa en fuegos artificiales.",ReactionEffect.Fire,"69F0AE"),
            E("Cs","Cl","2Cs + Cl2 -> 2CsCl","Cloruro de cesio","CsCl","El cesio es tan reactivo que reacciona de forma violenta.",ReactionEffect.Explosion,"90CAF9"),
            E("Fe","O","4Fe + 3O2 -> 2Fe2O3","Óxido de hierro (III) (herrumbre)","Fe2O3","Con humedad, el hierro se oxida lentamente y se forma la herrumbre rojiza. ¡Por eso se pintan los puentes!",ReactionEffect.Slow,"BF5B30"),
            E("Fe","S","Fe + S -> FeS","Sulfuro de hierro (II)","FeS","Al calentar limaduras de hierro con azufre, la mezcla brilla al rojo vivo y forma un compuesto nuevo.",ReactionEffect.Glow,"FF8F00"),
            E("Fe","Cl","2Fe + 3Cl2 -> 2FeCl3","Cloruro de hierro (III)","FeCl3","El hierro caliente arde en cloro con chispas anaranjadas.",ReactionEffect.Fire,"FFA000"),
            E("Fe","C","Fe + C -> Acero","Acero (aleación)","","Un poco de carbono mezclado con hierro fundido forma el acero, mucho más duro y resistente.",ReactionEffect.Alloy,"B0BEC5"),
            E("Cu","O","2Cu + O2 -> 2CuO","Óxido de cobre (II)","CuO","Al calentarlo, el cobre se cubre de una capa negra de óxido.",ReactionEffect.Glow,"4E342E"),
            E("Cu","S","2Cu + S -> Cu2S","Sulfuro de cobre (I)","Cu2S","El cobre caliente se une al azufre. Muchos minerales de cobre del Perú son sulfuros.",ReactionEffect.Glow,"FF8F00"),
            E("Cu","Cl","Cu + Cl2 -> CuCl2","Cloruro de cobre (II)","CuCl2","El cobre arde en cloro. Sus compuestos dan llama verde azulada.",ReactionEffect.Fire,"26C6DA"),
            E("Cu","Zn","Cu + Zn -> Latón","Latón (aleación)","","El latón es dorado y se usa en instrumentos musicales, llaves y grifería.",ReactionEffect.Alloy,"E6C35C"),
            E("Cu","Sn","Cu + Sn -> Bronce","Bronce (aleación)","","El bronce cambió la historia: dio nombre a la Edad del Bronce y se usa en estatuas y campanas.",ReactionEffect.Alloy,"CD7F32"),
            E("Zn","S","Zn + S -> ZnS","Sulfuro de zinc","ZnS","Reacción muy vigorosa con un destello y humo. El sulfuro de zinc brilla en la oscuridad.",ReactionEffect.Fire,"FFF176"),
            E("Zn","O","2Zn + O2 -> 2ZnO","Óxido de zinc","ZnO","El óxido de zinc es un polvo blanco usado en protectores solares y pomadas.",ReactionEffect.Fire,"E0F7FA"),
            E("Al","O","4Al + 3O2 -> 2Al2O3","Óxido de aluminio","Al2O3","Forma una capa invisible que protege al aluminio. En forma de cristal es el rubí y el zafiro.",ReactionEffect.Slow,"ECEFF1"),
            E("Al","Br","2Al + 3Br2 -> 2AlBr3","Bromuro de aluminio","AlBr3","El aluminio reacciona con el bromo líquido lanzando chispas y humo.",ReactionEffect.Fire,"FF7043"),
            E("Al","I","2Al + 3I2 -> 2AlI3","Yoduro de aluminio","AlI3","Con una gota de agua la reacción se enciende y libera un vapor violeta de yodo.",ReactionEffect.Smoke,"AB47BC"),
            E("C","O","C + O2 -> CO2","Dióxido de carbono","CO2","Cuando el carbón arde produce dióxido de carbono, el gas que exhalas y que usan las plantas.",ReactionEffect.Fire,"FF7043"),
            E("C","S","C + 2S -> CS2","Disulfuro de carbono","CS2","A alta temperatura forman un líquido muy inflamable usado en la industria.",ReactionEffect.Glow,"FFF59D"),
            E("C","Si","Si + C -> SiC","Carburo de silicio","SiC","Forma un material casi tan duro como el diamante, usado en lijas y frenos.",ReactionEffect.Glow,"80CBC4"),
            E("C","W","W + C -> WC","Carburo de wolframio","WC","Un material durísimo usado en brocas y herramientas de corte.",ReactionEffect.Glow,"90A4AE"),
            E("S","O","S + O2 -> SO2","Dióxido de azufre","SO2","El azufre arde con una bonita llama azul y forma un gas irritante que causa la lluvia ácida.",ReactionEffect.Fire,"448AFF"),
            E("N","O","N2 + O2 -> 2NO","Monóxido de nitrógeno","NO","Solo ocurre a temperaturas altísimas, como dentro de un rayo o un motor.",ReactionEffect.BrightLight,"B3E5FC"),
            E("P","O","4P + 5O2 -> P4O10","Óxido de fósforo (V)","P4O10","El fósforo blanco arde solo en el aire con una llama brillante y mucho humo blanco.",ReactionEffect.Fire,"FFF8E1"),
            E("Si","O","Si + O2 -> SiO2","Dióxido de silicio (sílice)","SiO2","Es el componente de la arena y el cuarzo, y la base del vidrio.",ReactionEffect.Slow,"E0F2F1"),
            E("B","N","2B + N2 -> 2BN","Nitruro de boro","BN","Forma un material casi tan duro como el diamante y que resiste muchísimo calor.",ReactionEffect.Glow,"FFFFFF"),
            E("Ag","S","2Ag + S -> Ag2S","Sulfuro de plata","Ag2S","Por eso las joyas de plata se oscurecen con el tiempo.",ReactionEffect.Slow,"424242"),
            E("Ag","Cl","2Ag + Cl2 -> 2AgCl","Cloruro de plata","AgCl","El cloruro de plata se oscurece con la luz: así funcionaba la fotografía antigua.",ReactionEffect.Slow,"EEEEEE"),
            E("Au","Hg","Au + Hg -> Amalgama","Amalgama de oro","","El mercurio disuelve el oro. En la minería ilegal se usa así y luego se evapora el mercurio, contaminando ríos de Madre de Dios.",ReactionEffect.Alloy,"FFD54F"),
            E("Hg","S","Hg + S -> HgS","Sulfuro de mercurio (cinabrio)","HgS","Forma el cinabrio, un mineral rojo que en Huancavelica se extraía desde la Colonia.",ReactionEffect.Slow,"E53935"),
            E("Pb","O","2Pb + O2 -> 2PbO","Óxido de plomo (II)","PbO","Al calentar plomo se forma un polvo amarillo-anaranjado.",ReactionEffect.Glow,"FFB300"),
            E("Sn","O","Sn + O2 -> SnO2","Óxido de estaño (IV)","SnO2","El óxido de estaño es el principal mineral del que se extrae el estaño.",ReactionEffect.Glow,"FFFFFF"),
            E("Ti","O","Ti + O2 -> TiO2","Dióxido de titanio","TiO2","Es el pigmento blanco de las pinturas y protectores solares.",ReactionEffect.BrightLight,"FFFFFF"),
            E("Cr","O","4Cr + 3O2 -> 2Cr2O3","Óxido de cromo (III)","Cr2O3","Forma una capa verde muy delgada que protege al acero inoxidable.",ReactionEffect.Slow,"43A047"),
            E("Xe","F","Xe + F2 -> XeF2","Difluoruro de xenón","XeF2","¡Un gas noble que sí reacciona! El flúor es tan reactivo que logra unirse al xenón.",ReactionEffect.Glow,"B388FF"),
        };

        static Entry E(string a, string b, string eq, string name, string formula, string desc, ReactionEffect fx, string color) =>
            new Entry { A = a, B = b, Eq = eq, Name = name, Formula = formula, Desc = desc, Fx = fx, Color = color };

        public static ReactionResult React(ElementInfo a, ElementInfo b)
        {
            var c = Curated.FirstOrDefault(r => (r.A == a.Symbol && r.B == b.Symbol) || (r.A == b.Symbol && r.B == a.Symbol));
            if (c != null)
            {
                return new ReactionResult
                {
                    Equation = c.Eq, ProductName = c.Name, Formula = c.Formula, Description = c.Desc,
                    Effect = c.Fx, EffectColor = CategoryInfo.Hex(c.Color)
                };
            }
            return Predict(a, b);
        }

        static ReactionResult Predict(ElementInfo a, ElementInfo b)
        {
            if (a.Z == b.Z) return SameElement(a);

            if (a.IsSuperheavy || b.IsSuperheavy)
            {
                return new ReactionResult
                {
                    Equation = $"{a.Symbol} + {b.Symbol} -> ?",
                    ProductName = "Desconocido",
                    Description = "Los elementos superpesados se crean átomo por átomo y duran muy poco tiempo, así que casi no se conoce su química.",
                    Effect = ReactionEffect.None, Predicted = true
                };
            }

            if (a.Category == ElementCategory.NobleGas || b.Category == ElementCategory.NobleGas)
            {
                return new ReactionResult
                {
                    Equation = $"{a.Symbol} + {b.Symbol} -> no hay reacción",
                    ProductName = "No reaccionan",
                    Description = "Los gases nobles tienen su último nivel lleno de electrones: están \"satisfechos\" y casi nunca reaccionan.",
                    Effect = ReactionEffect.None, Predicted = true
                };
            }

            if (a.IsMetal && b.IsMetal)
            {
                return new ReactionResult
                {
                    Equation = $"{a.Symbol} + {b.Symbol} -> aleación",
                    ProductName = $"Aleación de {a.Name.ToLower()} y {b.Name.ToLower()}",
                    Description = "Dos metales no forman un compuesto nuevo: al fundirse juntos se mezclan y forman una aleación, como el bronce o el acero.",
                    Effect = ReactionEffect.Alloy, EffectColor = Color.Lerp(a.CpkColor, b.CpkColor, 0.5f), Predicted = true
                };
            }

            // El menos electronegativo actua como "cation" (valencia positiva), el otro como "anion".
            var cation = ElectronegativityValue(a) <= ElectronegativityValue(b) ? a : b;
            var anion = cation == a ? b : a;
            if (cation.Symbol == "H" && anion.IsMetal) { var t = cation; cation = anion; anion = t; }

            int vPos = cation.MainPositiveValence;
            int vNeg = -anion.MainNegativeValence;
            if (anion.Symbol == "H") vNeg = 1;
            if (vPos <= 0 || vNeg <= 0)
            {
                return new ReactionResult
                {
                    Equation = $"{a.Symbol} + {b.Symbol} -> ?",
                    ProductName = "Reacción poco común",
                    Description = "Estos elementos no suelen reaccionar directamente entre sí en condiciones normales.",
                    Effect = ReactionEffect.None, Predicted = true
                };
            }

            int g = Gcd(vPos, vNeg);
            int nCation = vNeg / g, nAnion = vPos / g;
            string formula = cation.Symbol + (nCation > 1 ? nCation.ToString() : "") + anion.Symbol + (nAnion > 1 ? nAnion.ToString() : "");
            string equation = Balance(cation, anion, nCation, nAnion, formula);

            string name = $"{AnionName(anion)} de {cation.Name.ToLower()}";
            if (cation.Valences.Count(v => v > 0) > 1) name += $" ({ToRoman(vPos)})";
            bool ionic = cation.IsMetal;
            float diff = Mathf.Abs(ElectronegativityValue(a) - ElectronegativityValue(b));
            var fx = diff > 2.2f ? ReactionEffect.Explosion : diff > 1.4f ? ReactionEffect.Fire : diff > 0.6f ? ReactionEffect.Glow : ReactionEffect.Slow;

            return new ReactionResult
            {
                Equation = equation,
                ProductName = Capitalize(name),
                Formula = formula,
                Description = ionic
                    ? $"El {cation.Name.ToLower()} cede electrones y el {anion.Name.ToLower()} los recibe: se forma un compuesto iónico. Usamos la valencia +{vPos} y la valencia -{vNeg} para cruzarlas."
                    : $"Ambos son no metales: comparten electrones y forman un compuesto covalente. Usamos la valencia +{vPos} y la valencia -{vNeg} para cruzarlas.",
                Effect = fx,
                EffectColor = Color.Lerp(cation.CpkColor, anion.CpkColor, 0.5f),
                Predicted = true
            };
        }

        static ReactionResult SameElement(ElementInfo e)
        {
            string desc;
            if (e.Symbol is "H" or "N" or "O" or "F" or "Cl" or "Br" or "I")
                desc = $"Dos átomos de {e.Name.ToLower()} se unen para formar la molécula {e.Symbol}<sub>2</sub>. Así se encuentra en la naturaleza.";
            else if (e.Category == ElementCategory.NobleGas)
                desc = "Los átomos de un gas noble no se unen ni siquiera entre ellos: viven sueltos.";
            else if (e.IsMetal)
                desc = "Los átomos de un metal se unen con un \"mar de electrones\" compartidos: eso es el enlace metálico.";
            else
                desc = "Los átomos de un mismo elemento pueden ordenarse de formas distintas (alótropos), como el diamante y el grafito.";
            return new ReactionResult
            {
                Equation = $"{e.Symbol} + {e.Symbol} -> {(desc.Contains("<sub>2") ? e.Symbol + "2" : e.Symbol)}",
                ProductName = "Mismo elemento",
                Formula = desc.Contains("<sub>2") ? e.Symbol + "2" : "",
                Description = desc,
                Effect = ReactionEffect.Glow,
                EffectColor = e.CpkColor,
                Predicted = true
            };
        }

        static readonly HashSet<string> Diatomic = new HashSet<string> { "H", "N", "O", "F", "Cl", "Br", "I" };

        static string Balance(ElementInfo cation, ElementInfo anion, int a, int b, string formula)
        {
            int m = Diatomic.Contains(cation.Symbol) ? 2 : 1;
            int x = Diatomic.Contains(anion.Symbol) ? 2 : 1;
            int p = Lcm(m / Gcd(a, m), x / Gcd(b, x));
            int cA = p * a / m, cB = p * b / x;
            string Coef(int n) => n > 1 ? n.ToString() : "";
            string sA = cation.Symbol + (m > 1 ? "2" : "");
            string sB = anion.Symbol + (x > 1 ? "2" : "");
            return $"{Coef(cA)}{sA} + {Coef(cB)}{sB} -> {Coef(p)}{formula}";
        }

        static string AnionName(ElementInfo e) => e.Symbol switch
        {
            "H" => "hidruro",
            "O" => "óxido",
            "S" => "sulfuro",
            "N" => "nitruro",
            "C" => "carburo",
            "P" => "fosfuro",
            "F" => "fluoruro",
            "Cl" => "cloruro",
            "Br" => "bromuro",
            "I" => "yoduro",
            "Se" => "seleniuro",
            "Te" => "telururo",
            "As" => "arseniuro",
            "Sb" => "antimoniuro",
            "Si" => "siliciuro",
            "B" => "boruro",
            "At" => "astaturo",
            _ => e.Name.ToLower() + "uro"
        };

        public static float ElectronegativityValue(ElementInfo e)
        {
            return float.TryParse(e.Electronegativity, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 1.5f;
        }

        static int Gcd(int a, int b) { while (b != 0) { var t = a % b; a = b; b = t; } return Mathf.Max(1, a); }
        static int Lcm(int a, int b) => a / Gcd(a, b) * b;
        static string Capitalize(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1);

        static string ToRoman(int n) => n switch
        {
            1 => "I", 2 => "II", 3 => "III", 4 => "IV", 5 => "V", 6 => "VI", 7 => "VII", 8 => "VIII", _ => n.ToString()
        };

        /// <summary>Convierte "2H2 + O2 -> 2H2O" en texto con subindices para TextMeshPro.</summary>
        public static string FormatEquation(string eq)
        {
            var sb = new StringBuilder();
            foreach (var token in eq.Split(' '))
            {
                if (sb.Length > 0) sb.Append(' ');
                if (token == "->") { sb.Append("→"); continue; }
                if (token == "+" || !Regex.IsMatch(token, "[A-Z]")) { sb.Append(token); continue; }
                var m = Regex.Match(token, @"^(\d*)(.*)$");
                sb.Append(m.Groups[1].Value);
                sb.Append(FormatFormula(m.Groups[2].Value));
            }
            return sb.ToString();
        }

        public static string FormatFormula(string formula)
        {
            if (string.IsNullOrEmpty(formula)) return formula;
            // Palabras como "Acero" o "Latón" no se tocan.
            if (formula.Length > 3 && formula.Skip(1).All(char.IsLower)) return formula;
            return Regex.Replace(formula, @"(?<=[A-Za-z\)])(\d+)", "<sub>$1</sub>");
        }

        /// <summary>Cuenta los atomos de una formula simple (ej. Fe2O3 -> Fe:2, O:3).</summary>
        public static List<(string symbol, int count)> ParseFormula(string formula)
        {
            var result = new List<(string, int)>();
            if (string.IsNullOrEmpty(formula)) return result;
            foreach (Match m in Regex.Matches(formula, @"([A-Z][a-z]?)(\d*)"))
            {
                int n = m.Groups[2].Value.Length > 0 ? int.Parse(m.Groups[2].Value) : 1;
                result.Add((m.Groups[1].Value, n));
            }
            return result;
        }
    }
}

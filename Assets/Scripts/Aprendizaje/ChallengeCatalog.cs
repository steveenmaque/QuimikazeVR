using System;
using System.Collections.Generic;
using System.Linq;

namespace LabQuimicaVR
{
    public class ChallengeQuestion
    {
        public string Question, Explanation, Hint, Domain, Topic;
        public string[] Options;
        public int CorrectIndex;
    }

    /// <summary>
    /// Una pregunta a la vez, práctica y feedback explicativo: DOI 10.1016/j.tics.2010.09.003.
    /// La selección de tres preguntas y los umbrales son decisiones del producto pendientes de validación.
    /// </summary>
    public static class ChallengeCatalog
    {
        public static List<ChallengeQuestion> Generate(ElementInfo e, int attempt = 0)
        {
            if (e == null || e.Z < 1 || e.Z > 118) throw new ArgumentException("Elemento inválido", nameof(e));
            var rng = new Random(e.Z * 137 + 19 + Math.Max(0, attempt) % 100000 * 29);
            int variant = Math.Max(0, attempt) % 4;
            string category = $"[{CategoryInfo.ShortTag(e.Category)}] {CategoryInfo.Name(e.Category)}";
            var other = CategoryInfo.AllCategories.Where(c => c != e.Category).OrderBy(c => rng.Next()).Take(2).ToArray();
            var relative = ElementDatabase.All.Where(x => x.Category == e.Category && x.Z != e.Z).OrderBy(x => rng.Next()).First();
            var outsiders = ElementDatabase.All.Where(x => x.Category != e.Category).OrderBy(x => rng.Next()).Take(2).ToArray();
            var questions = new List<ChallengeQuestion>();
            string categoryHint = "Relaciona el código de su casilla con la categoría de la leyenda; una categoría puede incluir varios grupos.";
            switch (variant)
            {
                case 0:
                    questions.Add(Make("Clasificación", $"¿A qué categoría de la leyenda pertenece {e.Name} ({e.Symbol})?", category,
                        $"[{CategoryInfo.ShortTag(other[0])}] {CategoryInfo.Name(other[0])}", $"[{CategoryInfo.ShortTag(other[1])}] {CategoryInfo.Name(other[1])}",
                        $"{e.Symbol} está clasificado en {category}. Una categoría de la leyenda puede incluir varios grupos químicos.", categoryHint, rng));
                    break;
                case 1:
                    questions.Add(Make("Clasificación", $"¿Qué elemento está en la misma categoría de la leyenda que {e.Symbol}?", $"{relative.Name} ({relative.Symbol})",
                        $"{outsiders[0].Name} ({outsiders[0].Symbol})", $"{outsiders[1].Name} ({outsiders[1].Symbol})",
                        $"{e.Symbol} y {relative.Symbol} comparten la categoría {category}. Esto no implica pertenecer al mismo grupo o columna.", categoryHint, rng));
                    break;
                case 2:
                    questions.Add(Make("Clasificación", $"¿Qué código identifica la categoría de {e.Symbol} en la leyenda?", CategoryInfo.ShortTag(e.Category),
                        CategoryInfo.ShortTag(other[0]), CategoryInfo.ShortTag(other[1]),
                        $"El código {CategoryInfo.ShortTag(e.Category)} corresponde a {CategoryInfo.Name(e.Category)}. Es una ayuda de esta interfaz, no un símbolo químico.", categoryHint, rng));
                    break;
                default:
                    questions.Add(Make("Clasificación", $"¿Qué pareja coincide en categoría de la leyenda con {e.Name}?", $"{e.Symbol} y {relative.Symbol}",
                        $"{e.Symbol} y {outsiders[0].Symbol}", $"{e.Symbol} y {outsiders[1].Symbol}",
                        $"{e.Symbol} y {relative.Symbol} están clasificados en {category}; compartir categoría no significa compartir grupo ni ser el mismo elemento.", categoryHint, rng));
                    break;
            }
            int shells = e.Shells.Length;
            switch (variant)
            {
                case 0:
                    questions.Add(Make("Estructura", $"¿Cuántos protones tiene un átomo de {e.Name}?", $"{e.Z} protones", $"{e.Z + 1} protones", $"{e.Z + 2} protones",
                        $"El número atómico Z = {e.Z} define sus {e.Z} protones.", "Busca Z en la casilla o en Propiedades; Z cuenta protones.", rng)); break;
                case 1:
                    questions.Add(Make("Estructura", $"¿Cuántos electrones tiene un átomo neutro de {e.Symbol}?", $"{e.Z} electrones", $"{e.Z + 1} electrones", $"{e.Z + 2} electrones",
                        $"Un átomo neutro tiene tantos electrones como protones: {e.Z}. Un ion puede tener otra cantidad de electrones.", "Neutro significa igual cantidad de protones y electrones.", rng)); break;
                case 2:
                    questions.Add(Make("Estructura", $"En el modelo por capas, ¿cuántos niveles ocupados muestra {e.Symbol}?", $"{shells} niveles",
                        $"{(shells == 7 ? 5 : shells + 1)} niveles", $"{(shells == 1 ? 3 : shells - 1)} niveles",
                        $"Su configuración distribuye electrones en {shells} niveles ocupados. Bohr es un modelo didáctico: no son órbitas reales.",
                        "Cuenta niveles en Bohr y contrasta con electrones por nivel en Propiedades.", rng)); break;
                default:
                    questions.Add(Make("Estructura", $"Si un átomo tiene {e.Z} protones, ¿cuál es su número atómico?", $"Z = {e.Z}", $"Z = {e.Z + 1}", $"Z = {e.Z + 2}",
                        $"Z cuenta protones: {e.Z}. Cambiar el número de protones cambia la identidad del elemento.", "Número atómico y cantidad de protones representan la misma relación.", rng)); break;
            }
            // La ruta de elementos sin isótopos estables evalúa esta propiedad en su primer intento.
            int property = (variant + (e.IsRadioactive ? 2 : 0)) % 4;
            switch (property)
            {
                case 0:
                    if (e.State != 'S' && e.State != 'L' && e.State != 'G')
                        questions.Add(Make("Propiedades", $"¿Qué puedes concluir del estado físico de {e.Symbol} usando estos datos?", "Estado no confirmado en estos datos",
                            "Sólido medido con certeza", "Líquido medido con certeza", "La ficha no ofrece un estado confirmado. Una predicción no equivale a una medición.",
                            "Comprueba si la ficha muestra un dato o dice desconocido/no confirmado.", rng, "evidence"));
                    else
                    {
                        var states = new[] { "Sólido", "Líquido", "Gas" }.Where(s => s != e.StateName).ToArray();
                        questions.Add(Make("Propiedades", $"Según la ficha, ¿cuál es el estado de {e.Symbol} a 25 °C?", e.StateName, states[0], states[1],
                            $"La ficha indica {e.StateName.ToLower()} a 25 °C. Cambiar temperatura puede cambiar estado; no cambia el número de protones.",
                            "Lee Estado a 25 °C en Propiedades o en la vitrina.", rng, "state"));
                    }
                    break;
                case 1:
                    questions.Add(Make("Propiedades", $"¿En qué periodo (fila) se ubica {e.Symbol}?", $"Periodo {e.Period}", $"Periodo {e.Period % 7 + 1}", $"Periodo {(e.Period + 1) % 7 + 1}",
                        $"{e.Name} se ubica en el periodo {e.Period}. Los periodos son las siete filas principales de la tabla.", "Busca Periodo en la ficha y localiza su fila principal.", rng, "period")); break;
                case 2:
                    questions.Add(Make("Propiedades", $"¿Qué describe correctamente los isótopos de {e.Symbol}?",
                        e.IsRadioactive ? "No tiene isótopos estables" : "Tiene al menos un isótopo estable",
                        e.IsRadioactive ? "Tiene al menos un isótopo estable" : "No tiene isótopos estables",
                        "Su símbolo determina el número de neutrones",
                        e.Z == 83 ? "Se observó decaimiento alfa de Bi-209 (de Marcillac et al., 2003). El bismuto no tiene isótopos estables." :
                            (e.IsRadioactive ? "Este elemento se clasifica sin isótopos estables." : "Este elemento tiene al menos un isótopo estable, pero también puede tener radioisótopos.") +
                            " Los isótopos comparten protones y difieren en neutrones.", "Consulta ¿Tiene isótopos estables? en Propiedades. Estable no significa que todos sus isótopos lo sean.", rng, "isotopes")); break;
                default:
                    if (e.Group > 0)
                        questions.Add(Make("Propiedades", $"¿En qué grupo (columna 1–18) se ubica {e.Symbol}?", $"Grupo {e.Group}", $"Grupo {e.Group % 18 + 1}", $"Grupo {(e.Group + 1) % 18 + 1}",
                            $"En esta tabla, {e.Symbol} está en el grupo {e.Group}. Grupo y periodo representan columna y fila, respectivamente.", "Consulta Grupo en la ficha; no lo confundas con Periodo.", rng, "group"));
                    else
                        questions.Add(Make("Propiedades", $"¿Qué condición define que un átomo siga siendo {e.Symbol}?", $"Conservar {e.Z} protones", "Conservar siempre el mismo estado físico", "Conservar siempre la misma cantidad de neutrones",
                            $"La identidad de {e.Symbol} la definen sus {e.Z} protones. Los isótopos pueden tener diferentes neutrones y el estado físico puede cambiar.", "Relaciona la identidad del elemento con el número atómico Z.", rng, "identity"));
                    break;
            }
            return questions;
        }

        static ChallengeQuestion Make(string domain, string prompt, string correct, string wrong1, string wrong2,
            string explanation, string hint, Random rng, string topic = "")
        {
            var options = new[] { correct, wrong1, wrong2 };
            for (int i = options.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                string temp = options[i]; options[i] = options[j]; options[j] = temp;
            }
            return new ChallengeQuestion { Domain = domain, Question = prompt, Options = options,
                CorrectIndex = Array.IndexOf(options, correct), Explanation = explanation, Hint = hint, Topic = topic };
        }
    }
}

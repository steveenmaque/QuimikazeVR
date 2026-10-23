using TMPro;
using UnityEngine;

namespace LabQuimicaVR
{
    /// <summary>Pantalla con datos curiosos que cambian cada pocos segundos.</summary>
    public class FunFactsScreen : MonoBehaviour
    {
        public TextMeshPro text;
        public TextMeshPro counter;
        public float secondsPerFact = 9f;

        public static readonly string[] Facts =
        {
            "Todo lo que ves, incluido tú, está hecho de solo 118 tipos de átomos combinados de distintas formas.",
            "Si un átomo fuera del tamaño de un estadio de fútbol, su núcleo sería como una canica en el centro.",
            "Solo 2 elementos son líquidos a temperatura ambiente: el mercurio (Hg) y el bromo (Br).",
            "11 elementos son gases a temperatura ambiente; casi todos están en el lado derecho de la tabla.",
            "Dmitri Mendeléyev ordenó los elementos en 1869 y dejó espacios vacíos para elementos que aún no se conocían.",
            "El Perú es uno de los mayores productores de cobre, plata, zinc y oro del mundo.",
            "El oxígeno es el elemento más abundante de la corteza terrestre y de tu cuerpo.",
            "El hidrógeno y el helio se formaron en el Big Bang; casi todos los demás elementos se formaron en las estrellas.",
            "El oro de tus joyas se formó cuando chocaron estrellas de neutrones hace miles de millones de años.",
            "Los elementos de una misma columna (grupo) se comportan de forma parecida porque tienen los mismos electrones en su último nivel.",
            "Las filas se llaman periodos: el número de periodo te dice cuántos niveles de electrones tiene el átomo.",
            "El elemento 118, el oganesón, completa la tabla periódica tal como la conocemos hoy.",
            "Tu cuerpo tiene unos 7 mil cuatrillones de átomos, ¡más que estrellas en el universo visible!",
            "El silicio de los chips de tu celular sale de la arena común.",
            "Los fuegos artificiales usan elementos para sus colores: estroncio rojo, bario verde, sodio amarillo y cobre azul.",
        };

        int index;
        float timer;

        void Start()
        {
            index = Random.Range(0, Facts.Length);
            Show();
        }

        void Update()
        {
            timer += Time.deltaTime;
            float t = timer / secondsPerFact;
            if (text != null) text.alpha = Mathf.Clamp01(Mathf.Min(timer * 2f, (secondsPerFact - timer) * 2f));
            if (t >= 1f)
            {
                timer = 0f;
                index = (index + 1) % Facts.Length;
                Show();
            }
        }

        void Show()
        {
            if (text != null) text.text = Facts[index];
            if (counter != null) counter.text = $"Dato {index + 1} de {Facts.Length}";
        }
    }
}

using TMPro;
using UnityEngine;

namespace LabQuimicaVR
{
    /// <summary>Pantalla con datos curiosos que cambian cada pocos segundos.</summary>
    // Referencias de las ocho tarjetas: D01, D01, D01, Q01, Q02, Q03, Q04, D02. Consulta Para docentes.
    public class FunFactsScreen : MonoBehaviour
    {
        public TextMeshPro text;
        public TextMeshPro counter;
        public float secondsPerFact = 9f;

        public static readonly string[] Facts =
        {
            "El número atómico Z identifica un elemento: indica cuántos protones tiene su núcleo. Localiza Z=8 en la tabla.",
            "Las columnas se llaman grupos y se numeran del 1 al 18. Busca dos elementos de la misma columna.",
            "Los periodos son las siete filas principales. El sodio (Na) pertenece al periodo 3. ¿En qué fila está el magnesio?",
            "El peso atómico suele representar un promedio de isótopos. No indica exactamente cuántos neutrones tiene cada átomo.",
            "El bismuto no tiene isótopos estables. Su radiactividad natural es muy lenta, pero se ha medido.",
            "Una fórmula compatible con valencias es una propuesta formal: no demuestra que una reacción ocurra al mezclar dos sustancias.",
            "La síntesis del elemento Z=117 se publicó en 2010. El nombre teneso se aprobó después.",
            "El esquema de llenado electrónico tiene excepciones. Compara Cr y Cu: el modelo debe respetar sus configuraciones.",
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

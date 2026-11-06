using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace LabQuimicaVR
{
    /// <summary>
    /// Estacion diegetica "RETO DE LA SESIÓN (3 PREGUNTAS)" dentro del nivel del elemento (HU15).
    /// Genera dinamicamente 3 preguntas de opcion multiple sobre el elemento actual (familia/estructura,
    /// estado fisico/ubicacion y descubrimiento/propiedades) para evidenciar el aprendizaje del estudiante
    /// en cada sesion con retroalimentacion formativa inmediata y estrellas de dominio (★ a ★★★).
    /// </summary>
    public class ElementChallengeStation : MonoBehaviour
    {
        class QuizItem
        {
            public string Question;
            public string[] Options;
            public int CorrectIndex;
            public string Explanation;
        }

        ElementInfo element;
        readonly List<QuizItem> questions = new List<QuizItem>();
        readonly List<GazeButton> optionButtons = new List<GazeButton>();

        TextMeshPro stepText;
        TextMeshPro starsText;
        TextMeshPro questionText;
        TextMeshPro feedbackText;
        GazeButton nextButton;

        int currentStep;
        int mistakesInCurrent;
        int totalMistakes;
        bool stepSolved;
        bool challengeCompleted;

        public void Build(ElementInfo e)
        {
            element = e;
            LabFactory.ClearChildren(transform);
            optionButtons.Clear();
            questions.Clear();

            GenerateQuestions(e);

            var size = new Vector2(2.6f, 2.75f);
            var screen = LabFactory.Screen("PantallaReto", transform, Vector3.zero, Quaternion.identity, size,
                "RETO DE LA SESIÓN", Palette.Accent2);

            // Cabecera de progreso y medallas de dominio
            LabFactory.Card(screen, new Vector3(0, 0.90f, 0), new Vector2(size.x - 0.2f, 0.28f), new Color(0.11f, 0.22f, 0.35f));
            stepText = LabFactory.Text(screen, "PREGUNTA 1 DE 3", new Vector3(-0.48f, 0.90f, -0.02f), 0.085f, Palette.Accent,
                TextAlignmentOptions.Left, 1.35f, 0.22f, true);
            starsText = LabFactory.Text(screen, "<color=#FFC34D><b>● ● ●</b></color> <size=75%>3/3 pts</size>", new Vector3(0.55f, 0.90f, -0.02f), 0.095f, Color.white,
                TextAlignmentOptions.Right, 1.2f, 0.22f, true);

            // Enunciado de la pregunta
            LabFactory.Card(screen, new Vector3(0, 0.46f, 0), new Vector2(size.x - 0.2f, 0.50f), new Color(0.09f, 0.16f, 0.26f));
            questionText = LabFactory.Text(screen, "", new Vector3(0, 0.46f, -0.02f), 0.082f, Color.white,
                TextAlignmentOptions.Center, size.x - 0.32f, 0.46f, true);
            questionText.enableAutoSizing = true;
            questionText.fontSizeMax = 0.85f;
            questionText.fontSizeMin = 0.55f;

            // 3 botones de opcion multiple (A, B, C)
            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                float y = 0.02f - i * 0.31f;
                var btn = LabFactory.Button(screen, $"Opción {i + 1}", new Vector3(0, y, -0.03f), new Vector2(size.x - 0.24f, 0.25f), 0.082f);
                btn.OnClick += () => Answer(idx);
                optionButtons.Add(btn);
            }

            // Retroalimentacion formativa inmediata (GOV.UK / Nielsen H9 / Norman)
            LabFactory.Card(screen, new Vector3(0, -0.92f, 0), new Vector2(size.x - 0.2f, 0.34f), new Color(0.10f, 0.18f, 0.28f));
            feedbackText = LabFactory.Text(screen, "Mira las paredes del laboratorio para investigar y elige la respuesta correcta con el botón A.",
                new Vector3(0, -0.92f, -0.02f), 0.072f, Palette.Text, TextAlignmentOptions.Center, size.x - 0.30f, 0.30f);
            feedbackText.enableAutoSizing = true;
            feedbackText.fontSizeMax = 0.72f;
            feedbackText.fontSizeMin = 0.48f;

            nextButton = LabFactory.Button(screen, "Siguiente pregunta »", new Vector3(0, -1.23f, -0.03f), new Vector2(1.85f, 0.22f), 0.078f, new Color(0.0f, 0.45f, 0.58f));
            nextButton.OnClick += NextStep;
            nextButton.gameObject.SetActive(false);

            currentStep = 0;
            totalMistakes = 0;
            challengeCompleted = false;
            ShowCurrentQuestion();
        }

        void GenerateQuestions(ElementInfo e)
        {
            // Pregunta 1: Familia quimica y codigo WCAG de 3 letras (conecta con Tarea T2 y la pizarra de Propiedades)
            string correctFam = $"[{CategoryInfo.ShortTag(e.Category)}] {CategoryInfo.Name(e.Category)}";
            var otherCats = CategoryInfo.AllCategories.Where(c => c != e.Category).ToList();
            var rng = new System.Random(e.Z * 137 + 19);
            var wrongCat1 = otherCats[rng.Next(otherCats.Count)];
            otherCats.Remove(wrongCat1);
            var wrongCat2 = otherCats[rng.Next(otherCats.Count)];
            string[] opts1 = { correctFam, $"[{CategoryInfo.ShortTag(wrongCat1)}] {CategoryInfo.Name(wrongCat1)}", $"[{CategoryInfo.ShortTag(wrongCat2)}] {CategoryInfo.Name(wrongCat2)}" };
            int corr1 = ShuffleOptions(opts1, rng, 0);

            questions.Add(new QuizItem
            {
                Question = $"1. Según su casilla y la pared de Propiedades, ¿a qué familia química pertenece {e.Article} {e.Name.ToLower()} ({e.Symbol})?",
                Options = opts1,
                CorrectIndex = corr1,
                Explanation = $"¡Correcto! {char.ToUpper(e.Article[0]) + e.Article.Substring(1)} {e.Name.ToLower()} pertenece a la familia {correctFam} (Grupo {PeriodicLayout.GroupLabel(e)}, Periodo {e.Period})."
            });

            // Pregunta 2: Estructura atomica (protones / niveles de energia en el Modelo de Bohr)
            string correctAtom = $"{e.Z} protones y {e.Shells.Length} {(e.Shells.Length == 1 ? "nivel" : "niveles")} de energía";
            int fakeZ1 = Mathf.Clamp(e.Z + (e.Z < 110 ? 4 : -4), 1, 118);
            int fakeShells1 = Mathf.Clamp(e.Shells.Length == 7 ? 5 : e.Shells.Length + 1, 1, 7);
            int fakeZ2 = Mathf.Clamp(e.Z > 3 ? e.Z - 2 : e.Z + 2, 1, 118);
            int fakeShells2 = Mathf.Clamp(e.Shells.Length > 1 ? e.Shells.Length - 1 : 3, 1, 7);
            string[] opts2 =
            {
                correctAtom,
                $"{fakeZ1} protones y {fakeShells1} niveles de energía",
                $"{fakeZ2} protones y {fakeShells2} {(fakeShells2 == 1 ? "nivel" : "niveles")} de energía"
            };
            int corr2 = ShuffleOptions(opts2, rng, 0);

            questions.Add(new QuizItem
            {
                Question = $"2. Observa el modelo atómico central: ¿cuántos protones (Z) y niveles de electrones tiene el átomo de {e.Name.ToLower()}?",
                Options = opts2,
                CorrectIndex = corr2,
                Explanation = $"¡Excelente! Su número atómico es Z = {e.Z} ({e.Z} protones y {e.Z} electrones repartidos en {e.Shells.Length} capas)."
            });

            // Pregunta 3: Estado fisico a 25 °C y radiactividad (conecta con Vitrina y Propiedades)
            string correctState = $"{e.StateName} a 25 °C · {(e.IsRadioactive ? "Radiactivo" : "No radiactivo")}";
            string altState1 = e.StateName == "Sólido" ? "Gas" : "Sólido";
            string altState2 = e.StateName == "Líquido" ? "Gas" : "Líquido";
            string[] opts3 =
            {
                correctState,
                $"{altState1} a 25 °C · {(e.IsRadioactive ? "No radiactivo" : "Radiactivo")}",
                $"{altState2} a 25 °C · {(e.IsRadioactive ? "Radiactivo" : "No radiactivo")}"
            };
            int corr3 = ShuffleOptions(opts3, rng, 0);

            questions.Add(new QuizItem
            {
                Question = $"3. Según la vitrina real y la tabla de propiedades, ¿cómo se encuentra {e.Article} {e.Name.ToLower()} a 25 °C?",
                Options = opts3,
                CorrectIndex = corr3,
                Explanation = $"¡Muy bien! A temperatura ambiente (25 °C) es {e.StateName.ToLower()} y {(e.IsRadioactive ? "es un elemento radiactivo" : "no es radiactivo")}."
            });
        }

        static int ShuffleOptions(string[] opts, System.Random rng, int correctOriginalIndex)
        {
            string correctVal = opts[correctOriginalIndex];
            for (int i = opts.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (opts[i], opts[j]) = (opts[j], opts[i]);
            }
            string[] prefixes = { "A) ", "B) ", "C) " };
            int newCorrect = 0;
            for (int i = 0; i < opts.Length; i++)
            {
                if (opts[i] == correctVal) newCorrect = i;
                opts[i] = prefixes[i] + opts[i];
            }
            return newCorrect;
        }

        void ShowCurrentQuestion()
        {
            if (currentStep >= questions.Count)
            {
                ShowSummary();
                return;
            }
            var q = questions[currentStep];
            stepSolved = false;
            mistakesInCurrent = 0;
            stepText.text = $"PREGUNTA {currentStep + 1} DE {questions.Count}";
            UpdateStarsPreview();
            questionText.text = q.Question;
            for (int i = 0; i < optionButtons.Count; i++)
            {
                optionButtons[i].gameObject.SetActive(true);
                optionButtons[i].interactable = true;
                optionButtons[i].SetSelected(false);
                if (optionButtons[i].Label != null) optionButtons[i].Label.text = q.Options[i];
            }
            feedbackText.text = "Investiga en los paneles de la sala y pulsa A sobre la opción correcta.";
            feedbackText.color = Palette.Text;
            nextButton.gameObject.SetActive(false);
        }

        public void Answer(int optionIndex)
        {
            if (challengeCompleted || stepSolved || currentStep >= questions.Count) return;
            var q = questions[currentStep];
            if (optionIndex == q.CorrectIndex)
            {
                stepSolved = true;
                optionButtons[optionIndex].SetSelected(true);
                for (int i = 0; i < optionButtons.Count; i++)
                    optionButtons[i].interactable = false;

                feedbackText.text = $"<color=#5FD6A8><b>{q.Explanation}</b></color>";
                LabAudio.Play(LabSound.Chime, 0.8f);
                nextButton.gameObject.SetActive(true);
                if (nextButton.Label != null)
                    nextButton.Label.text = currentStep < questions.Count - 1 ? "Siguiente pregunta »" : "Ver logro de sesión »";
            }
            else
            {
                mistakesInCurrent++;
                totalMistakes++;
                optionButtons[optionIndex].interactable = false;
                UpdateStarsPreview();
                feedbackText.text = "<color=#FFC34D><b>Pista:</b> Revisa el panel de Propiedades (derecha) o el modelo central e inténtalo de nuevo.</color>";
                LabAudio.Play(LabSound.Click, 0.5f);
            }
        }

        void NextStep()
        {
            if (challengeCompleted)
            {
                Build(element);
                return;
            }
            if (!stepSolved) return;
            currentStep++;
            ShowCurrentQuestion();
        }

        public static string FormatMedals(int earned)
        {
            earned = Mathf.Clamp(earned, 0, 3);
            var parts = new string[3];
            for (int i = 0; i < 3; i++)
                parts[i] = i < earned ? "<color=#FFC34D>●</color>" : "<color=#5A6E85>●</color>";
            return string.Join(" ", parts);
        }

        public void SetDemoState(ElementInfo e, bool solvedFirstQuestion)
        {
            Build(e);
            if (solvedFirstQuestion && questions.Count > 0)
                Answer(questions[0].CorrectIndex);
        }

        void UpdateStarsPreview()
        {
            int stars = Mathf.Clamp(3 - totalMistakes, 1, 3);
            starsText.text = $"<b>{FormatMedals(stars)}</b> <size=75%>{stars}/3 pts</size>";
        }

        void ShowSummary()
        {
            challengeCompleted = true;
            int earnedStars = Mathf.Clamp(3 - totalMistakes, 1, 3);
            LabState.RecordElementChallenge(element.Z, earnedStars);

            string medalIcons = FormatMedals(earnedStars);
            stepText.text = "¡RETO COMPLETADO!";
            starsText.text = $"<b>{medalIcons}</b> <size=75%>{earnedStars}/3 pts</size>";
            questionText.text = $"<color=#5FD6A8><b>¡Dominaste {element.Article} {element.Name.ToLower()} ({element.Symbol})!</b></color>\n" +
                                $"Logro obtenido: <b>{earnedStars} de 3 medallas ({medalIcons})</b> en este elemento.";

            foreach (var b in optionButtons) b.gameObject.SetActive(false);

            feedbackText.text = $"Progreso de la sesión: <b>{LabState.MasteredElementsCount} elementos superados</b> · <b>{LabState.TotalStars} medallas</b> · Rango: <color=#00E5FF><b>{LabState.StudentRank}</b></color>";
            nextButton.gameObject.SetActive(true);
            if (nextButton.Label != null) nextButton.Label.text = "Repetir reto formativo »";
            LabAudio.Play(LabSound.Chime, 1.0f);
        }
    }
}

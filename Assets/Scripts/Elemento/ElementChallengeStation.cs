using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace LabQuimicaVR
{
    /// <summary>Práctica de recuperación, feedback explicativo y logros guardados en el dispositivo.</summary>
    public class ElementChallengeStation : MonoBehaviour
    {
        ElementInfo element;
        List<ChallengeQuestion> questions;
        readonly List<GazeButton> optionButtons = new List<GazeButton>();
        TextMeshPro stepText, starsText, questionText, feedbackText;
        GazeButton nextButton;
        int currentStep, mistakesInCurrent, totalMistakes, firstTryCorrect;
        bool stepSolved, challengeCompleted, demo;

        public void Build(ElementInfo e)
        {
            element = e;
            LabFactory.ClearChildren(transform);
            optionButtons.Clear();
            int attempt = Application.isPlaying && !demo ? LearningProgress.GetAttempts(e.Z) : 0;
            questions = ChallengeCatalog.Generate(e, attempt);
            var size = new Vector2(2.22f, 3.6f);
            var screen = LabFactory.Screen("PantallaReto", transform, Vector3.zero, Quaternion.identity, size,
                "RETO DE APRENDIZAJE", Palette.Accent2);
            stepText = LabFactory.Text(screen, "", new Vector3(-0.42f, 1.16f, -0.02f), 0.09f, Palette.Accent,
                TextAlignmentOptions.Left, 1.16f, 0.18f, true);
            starsText = LabFactory.Text(screen, "", new Vector3(0.62f, 1.16f, -0.02f), 0.09f, Palette.Good,
                TextAlignmentOptions.Right, 0.76f, 0.18f, true);
            questionText = LabFactory.Text(screen, "", new Vector3(0, 0.80f, -0.02f), 0.095f, Color.white,
                TextAlignmentOptions.Center, 1.96f, 0.50f, true);
            Fit(questionText, 0.95f, 0.66f);
            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                var btn = LabFactory.Button(screen, "", new Vector3(0, 0.32f - i * 0.36f, -0.03f), new Vector2(1.96f, 0.30f), 0.085f);
                btn.OnClick += () => Answer(idx);
                Fit(btn.Label, 0.85f, 0.60f);
                optionButtons.Add(btn);
            }
            LabFactory.Card(screen, new Vector3(0, -0.97f, 0), new Vector2(2.02f, 0.68f), new Color(0.10f, 0.18f, 0.28f));
            feedbackText = LabFactory.Text(screen, "", new Vector3(0, -0.97f, -0.02f), 0.08f, Palette.Text,
                TextAlignmentOptions.Center, 1.90f, 0.62f);
            Fit(feedbackText, 0.80f, 0.57f);
            nextButton = LabFactory.Button(screen, "Siguiente pregunta ›", new Vector3(0, -1.54f, -0.03f), new Vector2(1.96f, 0.30f), 0.09f);
            nextButton.OnClick += NextStep;
            currentStep = totalMistakes = firstTryCorrect = 0;
            challengeCompleted = false;
            ShowCurrentQuestion();
        }

        static void Fit(TextMeshPro text, float max, float min)
        {
            if (text == null) return;
            text.enableAutoSizing = true;
            text.fontSizeMax = max;
            text.fontSizeMin = min;
            text.overflowMode = TextOverflowModes.Ellipsis;
        }

        void ShowCurrentQuestion()
        {
            if (currentStep >= questions.Count) { ShowSummary(); return; }
            var q = questions[currentStep];
            stepSolved = false;
            mistakesInCurrent = 0;
            stepText.text = $"{currentStep + 1}/3 · {q.Domain}";
            starsText.text = $"{firstTryCorrect} sin pista";
            questionText.text = q.Question;
            for (int i = 0; i < optionButtons.Count; i++)
            {
                optionButtons[i].gameObject.SetActive(true);
                optionButtons[i].interactable = true;
                optionButtons[i].SetSelected(false);
                optionButtons[i].Label.text = "ABC"[i] + ") " + q.Options[i];
            }
            feedbackText.text = "Investiga en la sala y responde. Tras cada respuesta podrás revisar la explicación. Sin límite de tiempo.";
            feedbackText.color = Palette.Text;
            nextButton.gameObject.SetActive(false);
        }

        public void Answer(int optionIndex)
        {
            if (questions == null || challengeCompleted || stepSolved || currentStep >= questions.Count ||
                optionIndex < 0 || optionIndex >= optionButtons.Count || !optionButtons[optionIndex].interactable) return;
            var q = questions[currentStep];
            if (optionIndex == q.CorrectIndex)
            {
                stepSolved = true;
                if (mistakesInCurrent == 0) firstTryCorrect++;
                optionButtons[optionIndex].SetSelected(true);
                foreach (var b in optionButtons) b.interactable = false;
                feedbackText.text = "<color=#5FD6A8><b>Respuesta correcta.</b></color> " + q.Explanation;
                starsText.text = $"{firstTryCorrect} sin pista";
                LabAudio.Play(LabSound.Chime, 0.8f);
                nextButton.gameObject.SetActive(true);
                nextButton.Label.text = currentStep < questions.Count - 1 ? "Siguiente pregunta ›" : "Ver resultado ›";
            }
            else
            {
                mistakesInCurrent++;
                totalMistakes++;
                optionButtons[optionIndex].interactable = false;
                feedbackText.text = "<color=#FFC34D><b>Revisa la idea:</b></color> " + q.Hint +
                    "\nLa respuesta es: " + q.Options[q.CorrectIndex] + ". Confírmala en el panel y selecciónala.";
                LabAudio.Play(LabSound.Click, 0.5f);
            }
        }

        void NextStep()
        {
            if (challengeCompleted) { Build(element); return; }
            if (!stepSolved) return;
            currentStep++;
            ShowCurrentQuestion();
        }

        public static string FormatMedals(int earned)
        {
            earned = Mathf.Clamp(earned, 0, 3);
            var parts = new string[3];
            for (int i = 0; i < 3; i++) parts[i] = i < earned ? "<color=#FFC34D>●</color>" : "<color=#5A6E85>●</color>";
            return string.Join(" ", parts);
        }

        public void SetDemoState(ElementInfo e, bool solvedFirstQuestion)
        {
            demo = true;
            Build(e);
            if (solvedFirstQuestion) Answer(questions[0].CorrectIndex);
        }

        void ShowSummary()
        {
            if (challengeCompleted) return;
            challengeCompleted = true;
            int stars = Mathf.Max(1, firstTryCorrect);
            ChallengeResult result = null;
            if (Application.isPlaying && !demo)
                result = LearningProgress.RecordChallenge(element.Z, firstTryCorrect, totalMistakes);
            stepText.text = "RETO COMPLETADO";
            starsText.text = FormatMedals(stars);
            questionText.text = $"<b>{element.Name}: {firstTryCorrect}/3 sin pistas</b>\n" +
                (firstTryCorrect >= 2 ? "Logro de práctica alcanzado." : "Con apoyo: vuelve a investigar y prueba otra vez.");
            foreach (var b in optionButtons) b.gameObject.SetActive(false);
            feedbackText.text = $"<b>{totalMistakes} errores revisados.</b> " +
                (result == null ? "Vista de demostración." : $"+{result.AwardedPoints} puntos. Mejor marca: {LearningProgress.GetStars(element.Z)}/3 medallas.\n") +
                "Repetir mejora tu práctica; los puntos solo aumentan al mejorar la mejor marca. Explica una propiedad a un compañero.";
            if (LearningProgress.LastSaveError != null) feedbackText.text = LearningProgress.LastSaveError;
            nextButton.gameObject.SetActive(true);
            nextButton.Label.text = "Repasar con otra variante ›";
            LabAudio.Play(LabSound.Chime, 1f);
        }
    }
}

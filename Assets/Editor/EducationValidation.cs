using System;
using System.Linq;
using UnityEngine;

namespace LabQuimicaVR.EditorTools
{
    /// <summary>Validación determinista; nunca modifica PlayerPrefs ni el progreso del estudiante.</summary>
    public static class EducationValidation
    {
        public static int Run()
        {
            int checks = 0;
            void Check(bool value, string message)
            {
                checks++;
                if (!value) throw new InvalidOperationException("Educación: " + message);
            }
            void Reject(Action action, string message)
            {
                bool rejected = false;
                try { action(); } catch (ArgumentException) { rejected = true; }
                Check(rejected, message);
            }

            Check(ElementDatabase.Count == 118, "la base debe contener 118 elementos");
            foreach (var e in ElementDatabase.All)
            {
                for (int variant = 0; variant < 12; variant++)
                {
                    var questions = ChallengeCatalog.Generate(e, variant);
                    Check(questions.Count == 3, $"Z{e.Z}: tres preguntas");
                    Check(questions.Select(q => q.Domain).Distinct().Count() == 3, $"Z{e.Z}: dominios distintos");
                    foreach (var q in questions)
                    {
                        Check(q.Options.Length == 3 && q.Options.Distinct().Count() == 3, $"Z{e.Z}: opciones únicas, variante {variant}");
                        Check(q.CorrectIndex >= 0 && q.CorrectIndex < 3, $"Z{e.Z}: índice correcto válido");
                        Check(!string.IsNullOrWhiteSpace(q.Question) && !string.IsNullOrWhiteSpace(q.Explanation) && !string.IsNullOrWhiteSpace(q.Hint),
                            $"Z{e.Z}: pregunta con explicación y pista");
                    }
                    if (questions[2].Topic == "evidence")
                        Check(questions[2].Options[questions[2].CorrectIndex] == "Estado no confirmado en estos datos", $"Z{e.Z}: no inventar estado físico");
                    if (e.State != 'S' && e.State != 'L' && e.State != 'G')
                        Check(questions[2].Topic != "state", $"Z{e.Z}: no evaluar un estado desconocido como medido");
                }
                var variants = Enumerable.Range(0, 8).Select(v => ChallengeCatalog.Generate(e, v)).ToArray();
                for (int domain = 0; domain < 3; domain++)
                    Check(variants.Select(q => q[domain].Question).Distinct().Count() >= 4, $"Z{e.Z}: cuatro enunciados realmente distintos por dominio");
                if (e.IsRadioactive) Check(variants[0][2].Topic == "isotopes", $"Z{e.Z}: primera evaluación de radiactividad alineada a ruta6");
            }

            var store = new LearningRecordStore();
            Check(store.TotalPoints == 0 && store.MasteredCount == 0, "inicio sin logros");
            Check(store.Explore(6) && store.TotalPoints == 5, "exploración suma una vez");
            Check(!store.Explore(6) && store.TotalPoints == 5, "explorar repetidamente no produce puntos");
            var first = store.Complete(6, 0, 3);
            Check(first.Stars == 1 && first.AwardedPoints == 25 && store.MasteredCount == 0, "práctica con apoyo no significa dominio");
            var improved = store.Complete(6, 2, 1);
            Check(improved.AwardedPoints == 25 && store.MasteredCount == 1 && store.TotalPoints == 55, "mejora suma solo diferencia");
            var repeat = store.Complete(6, 2, 1);
            Check(repeat.AwardedPoints == 0 && repeat.Attempt == 3, "sin farming al repetir");
            var worse = store.Complete(6, 0, 6);
            Check(worse.AwardedPoints == 0 && store.Find(6).Stars == 2, "un mal intento no borra la mejor marca");
            Check(store.Find(6).LastFirstTry == 0 && store.Find(6).LastMistakes == 6 && store.Find(6).TotalMistakes == 11,
                "el informe conserva errores e intentos reales");
            Reject(() => store.Explore(0), "rechazar Z0");
            Reject(() => store.Explore(119), "rechazar Z119");
            Reject(() => store.Complete(6, 4, 0), "rechazar aciertos fuera de rango");
            Reject(() => store.Complete(6, 0, 0), "rechazar un resultado inconsistente");
            Reject(() => store.Complete(6, 1, 7), "rechazar errores imposibles en tres preguntas");

            var roundtrip = JsonUtility.FromJson<LearningRecordStore>(JsonUtility.ToJson(store));
            roundtrip.Sanitize();
            Check(roundtrip.TotalPoints == store.TotalPoints && roundtrip.Find(6).Attempts == 4 && roundtrip.Find(6).TotalMistakes == 11,
                "persistencia JSON conserva puntos, intentos y errores");
            roundtrip.Records.Add(null);
            roundtrip.Records.Add(new ElementLearningRecord { Z = 999, Attempts = 10 });
            roundtrip.Records.Add(new ElementLearningRecord { Z = 6, Attempts = -2, BestFirstTry = -1 });
            roundtrip.Sanitize();
            Check(roundtrip.Records.Count == 1 && roundtrip.TotalPoints == 55, "recuperación de filas nulas, duplicadas e inválidas");
            roundtrip.Find(6).LastCompletedUtc = "=HYPERLINK(\"example\");\ninjected";
            string csv = LearningProgress.BuildCsv(roundtrip);
            Check(csv.Split('\n').Length == 120 && !csv.Contains("HYPERLINK") && !csv.Contains("injected"), "CSV118filas sin inyección desde timestamp corrupto");
            Check(csv.Split('\n').Where(l => l.Length > 0).All(l => l.Split(';').Length == 11), "CSVcon once columnas uniformes");

            store = new LearningRecordStore();
            Check(LearningRoutes.CurrentIndex(store) == 0 && LearningRoutes.CompletedCount(store) == 0, "primera ruta al inicio");
            foreach (int z in LearningRoutes.Missions[0].Elements) store.Complete(z, 2, 1);
            Check(LearningRoutes.Missions[0].Complete(store) && LearningRoutes.CurrentIndex(store) == 1, "desbloqueo por evidencia de logro");
            foreach (var mission in LearningRoutes.Missions)
            {
                Check(mission.Elements.Length == 3 && mission.Elements.Distinct().Count() == 3, "misión contiene tres elementos distintos");
                Check(mission.Elements.All(z => z >= 1 && z <= 118), "elementos de misión válidos");
                foreach (int z in mission.Elements) store.Complete(z, 3, 0);
            }
            Check(LearningRoutes.CompletedCount(store) == 6 && LearningRoutes.CurrentIndex(store) == 5, "final de campaña sin desbordamiento");
            foreach (var e in ElementDatabase.All) store.Complete(e.Z, 3, 0);
            Check(store.TotalPoints == 9440 && store.MasteredCount == 118 && store.CompletedCount == 118, "techo finito de puntuación para 118 elementos");
            for (int i = 0; i < 10; i++) store.Complete(6, 3, 0);
            Check(store.TotalPoints == 9440, "repeticiones no superan techo");
            Debug.Log("[EducationValidation] " + checks + " comprobaciones superadas.");
            return checks;
        }
    }
}

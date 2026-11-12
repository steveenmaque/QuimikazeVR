using UnityEngine;

namespace LabQuimicaVR
{
    /// <summary>Navegación y fachada de compatibilidad al progreso local persistente.</summary>
    public static class LabState
    {
        public const string TableScene = "TablaPeriodica";
        public const string ElementScene = "ElementoDetalle";

        public static int SelectedZ = 6;
        public static bool Visited;

        // Los resultados se guardan localmente en LearningProgress; nunca se envían a terceros.

        public static int TotalStars
        {
            get
            {
                return LearningProgress.TotalStars;
            }
        }

        public static int MasteredElementsCount => LearningProgress.MasteredCount;
        public static int ExploredElementsCount => LearningProgress.ExploredCount;

        public static int GetStars(int z) => LearningProgress.GetStars(z);

        public static void RecordElementChallenge(int z, int stars)
        {
            stars = Mathf.Clamp(stars, 1, 3);
            LearningProgress.RecordChallenge(z, stars, 3 - stars);
        }

        public static string StudentRank
        {
            get
            {
                int mastered = LearningProgress.MasteredCount;
                if (LearningRoutes.CompletedCount(LearningProgress.Store) == LearningRoutes.Missions.Length) return "Ruta de aula completada";
                if (mastered >= 18) return "Explorador avanzado";
                if (mastered >= 9) return "Investigador en práctica";
                if (mastered >= 3) return "Explorador con logros";
                return "Explorador inicial";
            }
        }

        public static void OpenElement(int z)
        {
            if (!LanClassroom.CanNavigate(ElementScene, Mathf.Clamp(z, 1, 118))) return;
            if (ScreenFader.Busy) return;
            SelectedZ = Mathf.Clamp(z, 1, 118);
            Visited = true;
            LearningProgress.RecordExploration(SelectedZ);
            LabAudio.Play(LabSound.Whoosh);
            ScreenFader.FadeToScene(ElementScene);
        }

        public static void BackToTable()
        {
            if (!LanClassroom.CanNavigate(TableScene, SelectedZ)) return;
            if (ScreenFader.Busy) return;
            LabAudio.Play(LabSound.Whoosh);
            ScreenFader.FadeToScene(TableScene);
        }
    }
}

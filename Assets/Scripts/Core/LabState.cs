using System.Collections.Generic;
using UnityEngine;

namespace LabQuimicaVR
{
    /// <summary>Estado compartido entre escenas, navegacion y progreso de aprendizaje por sesion.</summary>
    public static class LabState
    {
        public const string TableScene = "TablaPeriodica";
        public const string ElementScene = "ElementoDetalle";

        public static int SelectedZ = 6;
        public static bool Visited;

        // Progreso formativo de la sesion (gamificacion sin conexion / HU15)
        static readonly Dictionary<int, int> elementStars = new Dictionary<int, int>();
        static readonly HashSet<int> exploredElements = new HashSet<int>();

        public static int TotalStars
        {
            get
            {
                int sum = 0;
                foreach (var kv in elementStars) sum += kv.Value;
                return sum;
            }
        }

        public static int MasteredElementsCount => elementStars.Count;
        public static int ExploredElementsCount => exploredElements.Count;

        public static int GetStars(int z) => elementStars.TryGetValue(z, out int s) ? s : 0;

        public static void RecordElementChallenge(int z, int stars)
        {
            stars = Mathf.Clamp(stars, 1, 3);
            if (!elementStars.TryGetValue(z, out int prev) || stars > prev)
                elementStars[z] = stars;
        }

        public static string StudentRank
        {
            get
            {
                int s = TotalStars;
                if (s >= 9) return "Maestro Químico";
                if (s >= 6) return "Alquimista Experto";
                if (s >= 3) return "Investigador Junior";
                return "Explorador Novato";
            }
        }

        public static void OpenElement(int z)
        {
            if (ScreenFader.Busy) return;
            SelectedZ = Mathf.Clamp(z, 1, 118);
            Visited = true;
            exploredElements.Add(SelectedZ);
            LabAudio.Play(LabSound.Whoosh);
            ScreenFader.FadeToScene(ElementScene);
        }

        public static void BackToTable()
        {
            if (ScreenFader.Busy) return;
            LabAudio.Play(LabSound.Whoosh);
            ScreenFader.FadeToScene(TableScene);
        }
    }
}

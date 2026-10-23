using UnityEngine;

namespace LabQuimicaVR
{
    /// <summary>Estado compartido entre escenas y navegacion.</summary>
    public static class LabState
    {
        public const string TableScene = "TablaPeriodica";
        public const string ElementScene = "ElementoDetalle";

        public static int SelectedZ = 6;
        public static bool Visited;

        public static void OpenElement(int z)
        {
            if (ScreenFader.Busy) return;
            SelectedZ = Mathf.Clamp(z, 1, 118);
            Visited = true;
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

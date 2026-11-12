using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LabQuimicaVR
{
    /// <summary>Objetivos explícitos, decisiones de aprendizaje y evidencia local para el docente.</summary>
    public class LearningMissionBoard : MonoBehaviour
    {
        TextMeshPro missionText, progressText, statusText;
        GazeButton startButton;
        int page;
        string displayedSaveError;
        const string DefaultStatus = "Sin límite de tiempo. Puedes explorar los 118 elementos libremente.\nPropuesta didáctica: el docente debe validar los logros.";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
            SceneManager.sceneLoaded += SceneLoaded;
        }

        static void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != LabState.TableScene || FindAnyObjectByType<LearningMissionBoard>() != null) return;
            var go = new GameObject("RutaDeAprendizaje");
            go.transform.position = new Vector3(-5.5f, 2.1f, 4.0f);
            var facing = go.transform.position - new Vector3(0, 2.1f, 1.5f);
            go.transform.rotation = Quaternion.LookRotation(facing, Vector3.up);
            go.AddComponent<LearningMissionBoard>();
        }

        void Start()
        {
            page = LearningRoutes.CurrentIndex(LearningProgress.Store);
            var screen = LabFactory.Screen("ObjetivosDelAula", transform, Vector3.zero, Quaternion.identity,
                new Vector2(3.4f, 3.5f), "TU RUTA DE QUÍMICA", Palette.Accent2);
            progressText = LabFactory.Text(screen, "", new Vector3(0, 1.07f, -0.02f), 0.105f, Palette.Good,
                TextAlignmentOptions.Center, 3.1f, 0.28f, true);
            missionText = LabFactory.Text(screen, "", new Vector3(0, 0.29f, -0.02f), 0.10f, Color.white,
                TextAlignmentOptions.TopLeft, 3.05f, 1.25f);
            missionText.enableAutoSizing = true;
            missionText.fontSizeMax = 1.0f;
            missionText.fontSizeMin = 0.72f;
            startButton = LabFactory.Button(screen, "Continuar misión", new Vector3(0, -0.56f, -0.03f), new Vector2(2.9f, 0.34f), 0.10f);
            startButton.OnClick += ContinueMission;
            var prev = LabFactory.Button(screen, "‹ Ruta", new Vector3(-0.98f, -1.0f, -0.03f), new Vector2(0.9f, 0.30f), 0.085f);
            var next = LabFactory.Button(screen, "Ruta ›", new Vector3(0.98f, -1.0f, -0.03f), new Vector2(0.9f, 0.30f), 0.085f);
            var export = LabFactory.Button(screen, "Informe", new Vector3(0, -1.0f, -0.03f), new Vector2(0.9f, 0.30f), 0.085f);
            prev.OnClick += () => { page = (page + LearningRoutes.Missions.Length - 1) % LearningRoutes.Missions.Length; Refresh(); };
            next.OnClick += () => { page = (page + 1) % LearningRoutes.Missions.Length; Refresh(); };
            export.OnClick += Export;
            statusText = LabFactory.Text(screen, DefaultStatus,
                new Vector3(0, -1.40f, -0.02f), 0.075f, Palette.Text, TextAlignmentOptions.Center, 3.05f, 0.45f);
            statusText.enableAutoSizing = true;
            statusText.fontSizeMax = 0.75f;
            statusText.fontSizeMin = 0.55f;
            LearningProgress.Changed += Refresh;
            Refresh();
        }

        void OnDestroy() { LearningProgress.Changed -= Refresh; }

        void Refresh()
        {
            if (missionText == null) return;
            var store = LearningProgress.Store;
            var mission = LearningRoutes.Missions[page];
            bool unlocked = page <= LearningRoutes.CurrentIndex(store);
            progressText.text = $"{LearningProgress.TotalPoints} puntos · {LearningProgress.MasteredCount}/118 con logro\n" +
                $"Rutas completadas: {LearningRoutes.CompletedCount(store)}/{LearningRoutes.Missions.Length}";
            string elements = "";
            foreach (int z in mission.Elements)
            {
                var e = ElementDatabase.Get(z);
                elements += $"{e.Symbol}: {LearningProgress.GetStars(z)}/3   ";
            }
            missionText.text = $"<b>{mission.Title}</b>\n{mission.Context}\n\n<b>Objetivo:</b> {mission.Objective}\n\n" +
                $"<b>Criterio propuesto:</b> 2 de 3 respuestas sin pistas en cada elemento.\n{elements}\n" +
                (unlocked ? "Investiga → responde → revisa la explicación → mejora." : "Completa las rutas anteriores para desbloquear esta misión.");
            startButton.interactable = unlocked;
            startButton.Label.text = mission.Complete(store) ? "Repasar misión" : unlocked ? "Continuar misión" : "Misión por desbloquear";
            if (LearningProgress.LastSaveError != null) statusText.text = LearningProgress.LastSaveError;
            else if (displayedSaveError != null) statusText.text = DefaultStatus;
            displayedSaveError = LearningProgress.LastSaveError;
        }

        void ContinueMission()
        {
            if (page > LearningRoutes.CurrentIndex(LearningProgress.Store)) return;
            var mission = LearningRoutes.Missions[page];
            int nextZ = mission.NextElement(LearningProgress.Store);
            LabState.OpenElement(nextZ == 0 ? mission.Elements[0] : nextZ);
        }

        void Export()
        {
            try
            {
                statusText.text = ReportShare.ExportAndShare();
            }
            catch (Exception ex)
            {
                statusText.text = "No se pudo guardar el informe. Libera espacio e inténtalo otra vez.";
                Debug.LogWarning("Informe docente: " + ex.Message);
            }
        }
    }
}

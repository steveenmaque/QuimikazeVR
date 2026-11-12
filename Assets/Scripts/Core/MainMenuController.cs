using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LabQuimicaVR
{
    /// <summary>Inicio y ajustes en una pared real de la sala; todos los controles usan mirada y visor/A.</summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        public const string TutorialPreferenceKey = "Quimikaze.Tutorial.v1";
        public static MainMenuController Instance { get; private set; }
        public static bool BlockMovement => Instance != null && !(Instance.CurrentPage == "tutorial" && Instance.TutorialStep == 2);
        public string CurrentPage { get; private set; } = "home";
        public int TutorialStep { get; private set; }
        public static bool TutorialComplete => PlayerPrefs.GetInt(TutorialPreferenceKey, 0) == 1;
        Vector3 walkStart;
        bool walked;
        bool shownInvertY, shownStereo;
        int shownOrientation;
        float shownLens;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            Instance = null;
            SceneManager.sceneLoaded -= SceneLoaded;
            SceneManager.sceneLoaded += SceneLoaded;
        }

        static void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != LabState.TableScene && scene.name != LabState.ElementScene) return;
            var wall = new GameObject("AccesoMenuPrincipal").transform;
            bool table = scene.name == LabState.TableScene;
            wall.position = table ? new Vector3(0, 0.23f, 11.94f) : new Vector3(0, 0.20f, -6.34f);
            wall.rotation = Quaternion.Euler(0, table ? 0 : 180, 0);
            var button = LabFactory.Button(wall, "Menú principal", Vector3.zero, new Vector2(3f, 0.28f), 0.12f);
            button.OnClick += LabState.BackToMenu;
        }

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }
        void Start() { ShowHome(); }

        void Update()
        {
            var rig = VRRig.Instance;
            if (CurrentPage == "options" && rig != null &&
                (shownInvertY != LabInput.InvertRightStickY || shownStereo != rig.Stereo ||
                 shownOrientation != VRRig.OrientationMode || !Mathf.Approximately(shownLens, rig.lensSeparationMm)))
                ShowOptions();
            if (CurrentPage == "lan" && LanClassroom.ActivePanel == null) { ShowHome(); return; }
            if (CurrentPage == "tutorial" && TutorialStep == 2 && !walked && VRRig.Instance != null)
            {
                Vector3 delta = VRRig.Instance.transform.position - walkStart;
                delta.y = 0;
                if (delta.sqrMagnitude >= 0.45f * 0.45f) { walked = true; DrawTutorial(); }
            }
            if (!LabInput.BackDown || LanClassroom.HandlesBackNavigation || ScientificLibrary.HandlesBackNavigation) return;
            if (CurrentPage == "home") ShowExit();
            else ShowHome();
        }

        Transform Page(string title)
        {
            LabFactory.ClearChildren(transform);
            return LabFactory.Screen("PantallaInicio", transform, Vector3.zero, Quaternion.identity,
                new Vector2(6.4f, 3.2f), title, Palette.Accent);
        }

        static TextMeshPro Text(Transform root, string value, float y, float height = 0.35f, float x = 0, float width = 5.8f, float size = 0.12f)
        {
            var text = LabFactory.Text(root, value, new Vector3(x, y, -0.025f), size, Palette.Text,
                TextAlignmentOptions.Center, width, height);
            text.enableAutoSizing = true;
            text.fontSizeMin = size * 6.5f;
            text.fontSizeMax = size * 10;
            return text;
        }

        static GazeButton Button(Transform root, string value, float x, float y, Action action, float width = 2.8f)
        {
            var button = LabFactory.Button(root, value, new Vector3(x, y, -0.04f), new Vector2(width, 0.42f), 0.13f);
            button.Label.enableAutoSizing = true;
            button.Label.fontSizeMin = 0.7f;
            button.Label.fontSizeMax = 1.3f;
            button.OnClick += action;
            return button;
        }

        void Back(Transform root) => Button(root, "Volver", 0, -1.28f, ShowHome, 2.1f);

        public void ShowHome()
        {
            CurrentPage = "home";
            var screen = Page("QUIMIKAZE");
            Text(screen, TutorialComplete ? "¡Tu próxima aventura te espera!" : "¿Primera vez? Empieza por Tutorial.", 0.88f, 0.28f);
            Button(screen, LanClassroom.CanResumeSession ? "Volver al aula" : "Jugar", -1.55f, 0.45f, EnterGame);
            Button(screen, "Tutorial", 1.55f, 0.45f, StartTutorial);
            Button(screen, "Aula LAN", -1.55f, -0.15f, ShowLan);
            Button(screen, "Mi personaje", 1.55f, -0.15f, ShowSkins);
            Button(screen, "Opciones", -1.55f, -0.75f, ShowOptions);
            Button(screen, "Salir", 1.55f, -0.75f, ShowExit);
            Text(screen, "Mira un botón y pulsa A o el botón del visor.\nPuedes volver aquí con B.", -1.30f, 0.34f, size:0.10f);
        }

        public void EnterGame()
        {
            if (LanClassroom.HasActiveSession)
            {
                if (LanClassroom.CanResumeSession) LanClassroom.EnterSharedLab();
                else ShowLan();
                return;
            }
            LabState.ResumeSoloLab();
        }

        public void ShowLan()
        {
            CurrentPage = "lan";
            LabFactory.ClearChildren(transform);
            var anchor = LabFactory.Node("AnclaAulaEnPared", transform, new Vector3(0, 0.15f, -0.03f)).transform;
            LanClassroom.OpenFromMenu(anchor);
        }

        public void ShowOptions()
        {
            CurrentPage = "options";
            var screen = Page("OPCIONES DEL VISOR");
            Text(screen, "Ajusta la vista a tu gusto. Todo se elige mirando.", 0.88f, 0.28f);
            var rig = VRRig.Instance;
            shownInvertY = LabInput.InvertRightStickY;
            shownStereo = rig != null && rig.Stereo;
            shownOrientation = VRRig.OrientationMode;
            shownLens = rig != null ? rig.lensSeparationMm : 63f;
            Button(screen, "Eje Y: " + (LabInput.InvertRightStickY ? "invertido" : "normal"), -1.55f, 0.45f,
                () => { VRRig.ToggleRightStickYAxis(); ShowOptions(); });
            Button(screen, "Vista: " + (rig != null && rig.Stereo ? "visor" : "una pantalla"), 1.55f, 0.45f,
                () => { if (rig != null) rig.SetStereo(!rig.Stereo); ShowOptions(); });
            Button(screen, "Lentes −", -1.55f, -0.15f, () => { rig?.AdjustLensSeparation(-1); ShowOptions(); });
            Button(screen, "Lentes +", 1.55f, -0.15f, () => { rig?.AdjustLensSeparation(1); ShowOptions(); });
            Text(screen, $"Separación de lentes: {shownLens:0} mm", -0.45f, 0.14f, size:0.075f);
            Button(screen, "Recentrar vista", -1.55f, -0.75f, () => rig?.Recenter());
            Button(screen, "Pantalla: " + VRRig.OrientationNames[VRRig.OrientationMode], 1.55f, -0.75f,
                () => { VRRig.CycleOrientation(); ShowOptions(); });
            Back(screen);
        }

        public void ShowSkins()
        {
            CurrentPage = "skins";
            var screen = Page("MI PERSONAJE");
            Text(screen, "Elige cómo te verán tus compañeros.", 0.88f, 0.28f);
            var podium = LabFactory.Node("VistaPreviaPersonaje", screen, new Vector3(-1.6f, -1.00f, -0.42f)).transform;
            LabFactory.Shape("BasePersonaje", PrimitiveType.Cylinder, podium, Vector3.zero,
                new Vector3(0.82f, 0.045f, 0.82f), LabFactory.Mat("Pedestal"));
            var model = AvatarSkins.BuildPreview(podium, AvatarSkins.SelectedIndex);
            model.localPosition = new Vector3(0, 0.06f, 0);
            model.localScale = Vector3.one * 0.88f;
            model.localRotation = Quaternion.Euler(0, 180f, 0);
            Text(screen, AvatarSkins.SelectedName, 0.30f, 0.5f, 1.5f, 2.6f, 0.18f);
            Text(screen, $"{AvatarSkins.SelectedIndex + 1} de {AvatarSkins.Count}\nTodos están disponibles.", -0.15f, 0.4f, 1.5f, 2.6f, 0.11f);
            Button(screen, "Anterior", 0.75f, -0.75f, () => ChangeSkin(-1), 1.3f);
            Button(screen, "Siguiente", 2.25f, -0.75f, () => ChangeSkin(1), 1.3f);
            Back(screen);
        }

        void ChangeSkin(int delta)
        {
            AvatarSkins.SelectedIndex = (AvatarSkins.SelectedIndex + delta + AvatarSkins.Count) % AvatarSkins.Count;
            ShowSkins();
        }

        public void StartTutorial()
        {
            TutorialStep = 0;
            CurrentPage = "tutorial";
            DrawTutorial();
        }

        void AdvanceTutorial()
        {
            TutorialStep++;
            if (TutorialStep == 2) { walkStart = VRRig.Instance != null ? VRRig.Instance.transform.position : Vector3.zero; walked = false; }
            DrawTutorial();
        }

        void DrawTutorial()
        {
            var screen = Page("APRENDE A JUGAR");
            Text(screen, $"Paso {TutorialStep + 1} de 4", 0.88f, 0.25f);
            switch (TutorialStep)
            {
                case 0:
                    Text(screen, "Mira el botón de la izquierda.\nCuando se ilumine, pulsa A o el botón del visor.", 0.30f, 0.65f);
                    Button(screen, "¡Lo encontré!", -1.55f, -0.65f, AdvanceTutorial);
                    break;
                case 1:
                    Text(screen, "¡Muy bien! Ahora busca el botón de la derecha.\nGira la cabeza o usa el joystick derecho.", 0.30f, 0.65f);
                    Button(screen, "¡Aquí está!", 1.55f, -0.65f, AdvanceTutorial);
                    break;
                case 2:
                    Text(screen, walked ? "¡Buen paso! Ya puedes continuar." :
                        "Con mando: avanza un paso hacia la marca usando\nel joystick izquierdo. Con solo el visor, elige la otra opción.", 0.30f, 0.70f);
                    var advance = Button(screen, "Continuar", -1.55f, -0.65f, AdvanceTutorial);
                    advance.interactable = walked;
                    Button(screen, "Uso solo el visor", 1.55f, -0.65f, AdvanceTutorial);
                    if (!walked)
                    {
                        var marker = LabFactory.Node("MarcaDePractica", transform).transform;
                        marker.position = walkStart + Vector3.forward * 0.70f + Vector3.up * 0.06f;
                        LabFactory.Shape("Marca", PrimitiveType.Cylinder, marker, Vector3.zero,
                            new Vector3(0.45f, 0.012f, 0.45f), LabFactory.Tinted("GlowBase", Palette.Good, 1.4f));
                    }
                    break;
                default:
                    Text(screen, "Explora y elige un elemento. Lee sus paneles y prueba el reto.\nTus misiones están en la pared izquierda de la sala.\nPulsa B o busca Menú principal para volver a los ajustes.", 0.22f, 1.05f);
                    Button(screen, "¡A explorar!", 0, -0.75f, () =>
                    {
                        PlayerPrefs.SetInt(TutorialPreferenceKey, 1);
                        PlayerPrefs.Save();
                        if (LanClassroom.HasActiveSession) EnterGame();
                        else LabState.BackToTable();
                    });
                    break;
            }
            Back(screen);
        }

        void ShowExit()
        {
            CurrentPage = "exit";
            var screen = Page("¿TERMINAMOS POR AHORA?");
            Text(screen, "Tus logros quedan guardados en este dispositivo.\nSi creaste un aula, al salir se cerrará para todos.", 0.25f, 0.8f);
            Button(screen, "Seguir jugando", -1.55f, -0.65f, ShowHome);
            Button(screen, "Salir del juego", 1.55f, -0.65f, () => Application.Quit());
        }
    }
}

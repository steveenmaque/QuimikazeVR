using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LabQuimicaVR
{
    /// <summary>Referencia docente optativa: acceso discreto y lectura fija en la pared, sin seguir a la cámara.</summary>
    public class ScientificLibrary : MonoBehaviour
    {
        public const string LauncherLabel = "Para docentes";
        // Tabla: hueco posterior de pared derecha; Datos ocupa z=[-2.2,3.0].
        // Detalle: hueco de pared izquierda z=[7.6,10.0], después de Historia.
        public static readonly Vector3 TableWallPosition = new Vector3(12.84f, 2.55f, -4.35f);
        public static readonly Vector3 ElementWallPosition = new Vector3(-4.84f, 2.65f, 8.8f);
        public static readonly Vector2 TablePanelSize = new Vector2(3.5f, 2.8f);
        public static readonly Vector2 ElementPanelSize = new Vector2(2.0f, 3.2f);

        public sealed class Entry
        {
            public readonly string Id, Citation, Url, Application;
            public Entry(string id, string citation, string url, string application)
            { Id = id; Citation = citation; Url = url; Application = application; }
        }

        public static readonly Entry[] Entries =
        {
            new Entry("P01", "Roediger y Karpicke (2006). Psychological Science, 17, 249–255.",
                "https://doi.org/10.1111/j.1467-9280.2006.01693.x",
                "Preguntar después de explorar permite practicar la recuperación de información. Este estudio no evalúa Quimikaze ni valida nuestra puntuación."),
            new Entry("P02", "Butler, Karpicke y Roediger (2007). Journal of Experimental Psychology: Applied, 13, 273–281.",
                "https://doi.org/10.1037/1076-898X.13.4.273",
                "La retroalimentación ayuda a revisar respuestas. Mostramos una explicación y permitimos reintentar. El estudio no establece que el feedback inmediato siempre sea mejor."),
            new Entry("P03", "Sailer y Homner (2020). Educational Psychology Review, 32, 77–112. Metaanálisis.",
                "https://doi.org/10.1007/s10648-019-09498-w",
                "La gamificación puede apoyar actividades de aprendizaje, con resultados variables. Las misiones y medallas son decisiones de diseño; acumular puntos no demuestra aprender."),
            new Entry("P04", "Makransky, Terkildsen y Mayer (2019). Learning and Instruction, 60, 225–236.",
                "https://doi.org/10.1016/j.learninstruc.2017.12.007",
                "Más inmersión no garantiza más aprendizaje. Presentamos una pregunta por vez y dejamos explorar al propio ritmo. Hace falta evaluar esta propuesta con estudiantes."),
            new Entry("P05", "Parong y Mayer (2018). Journal of Educational Psychology, 110, 785–797.",
                "https://doi.org/10.1037/edu0000241",
                "Segmentar y resumir apoyó una lección VR universitaria (Exp. 2). Respalda avanzar por pasos; no valida nuestro tutorial ni edades de 10–14 años."),
            new Entry("P06", "Yee y Bailenson (2007). Human Communication Research, 33, 271–290.",
                "https://doi.org/10.1111/j.1468-2958.2007.00299.x",
                "La apariencia del avatar influyó en la conducta interpersonal en VR. Justifica tratar el avatar con rigor; no prueba que las skins mejoren el aprendizaje."),
            new Entry("Q01", "Prohaska y colaboradores (2022). Pure and Applied Chemistry, 94, 573–600. Informe IUPAC.",
                "https://doi.org/10.1515/pac-2019-0603",
                "Referencia de pesos atómicos. Un promedio de masas no determina un isótopo: los neutrones del modelo son ilustrativos. Valores entre corchetes no son pesos atómicos estándar."),
            new Entry("Q02", "de Marcillac y colaboradores (2003). Nature, 422, 876–878.",
                "https://doi.org/10.1038/nature01541",
                "Se observó la desintegración alfa de Bi-209. El bismuto no tiene isótopos estables, aunque su desintegración sea extremadamente lenta."),
            new Entry("Q03", "Grimmel y Reiher (2021). On the Predictive Power of Chemical Concepts. CHIMIA, 75, 311–318.",
                "https://doi.org/10.2533/chimia.2021.311",
                "Los descriptores químicos tienen límites predictivos. Una fórmula sugerida por valencias no verifica que ocurra una reacción ni que produzca fuego o una explosión."),
            new Entry("Q04", "Oganessian y colaboradores (2010). Physical Review Letters, 104, 142502.",
                "https://doi.org/10.1103/PhysRevLett.104.142502",
                "Síntesis del elemento Z=117. Distingue la producción de un elemento de su reconocimiento y del nombre asignado posteriormente."),
            new Entry("D01", "IUPAC. Tabla periódica oficial. Fuente de datos complementaria.",
                "https://iupac.org/what-we-do/periodic-table-of-elements/",
                "Identidad de los 118 elementos, símbolos y organización periódica. Los grupos se numeran 1–18; los periodos son las siete filas principales."),
            new Entry("D02", "NIST. Atomic Spectra Database. SRD 78. Fuente de datos complementaria.",
                "https://doi.org/10.18434/T4W30F",
                "Referencia de estados fundamentales y configuraciones electrónicas. El llenado tiene excepciones. Bohr y la nube animada son modelos didácticos sin escala ni trayectorias físicas exactas.")
        };

        Transform panel;
        GazeButton launcher;
        int page;
        static ScientificLibrary instance;
        static int closedFrame = -1;
        public static Transform ActivePanel => instance != null ? instance.panel : null;
        public static bool HandlesBackNavigation => (instance != null && instance.panel != null) || closedFrame == Time.frameCount;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            instance = null;
            closedFrame = -1;
            SceneManager.sceneLoaded -= SceneLoaded;
            SceneManager.sceneLoaded += SceneLoaded;
        }

        static void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != LabState.TableScene && scene.name != LabState.ElementScene) return;
            if (FindAnyObjectByType<ScientificLibrary>() == null)
            {
                var go = new GameObject("FuentesCientificas");
                bool table = scene.name == LabState.TableScene;
                go.transform.SetPositionAndRotation(table ? TableWallPosition : ElementWallPosition,
                    Quaternion.Euler(0, table ? 90f : -90f, 0));
                go.AddComponent<ScientificLibrary>();
            }
        }

        void Awake() { instance = this; }
        void OnDestroy() { if (instance == this) instance = null; }
        void Update()
        {
            if (launcher != null) launcher.gameObject.SetActive(panel == null && LanClassroom.ActivePanel == null);
            if (panel != null && LabInput.BackDown) Close();
        }

        public void Close()
        {
            if (panel == null) return;
            panel.gameObject.SetActive(false);
            Destroy(panel.gameObject);
            panel = null;
            closedFrame = Time.frameCount;
            if (launcher != null) launcher.gameObject.SetActive(LanClassroom.ActivePanel == null);
        }

        void Start()
        {
            if (gameObject.scene.name != LabState.TableScene && gameObject.scene.name != LabState.ElementScene) return;
            bool table = gameObject.scene.name == LabState.TableScene;
            // Por defecto solo se muestra esta pequeña placa: ninguna cita ocupa el espacio de juego.
            launcher = LabFactory.Button(transform, LauncherLabel, new Vector3(0, table ? -0.9f : -0.95f, -0.025f),
                new Vector2(1.7f, 0.34f), 0.10f, new Color(0.20f, 0.29f, 0.38f));
            launcher.gameObject.name = "AccesoParaDocentes";
            launcher.OnClick += Open;
        }

        public void Open()
        {
            if (panel != null || LanClassroom.ActivePanel != null) return;
            if (gameObject.scene.name != LabState.TableScene && gameObject.scene.name != LabState.ElementScene) return;
            // Misma pared que el acceso; abrir no depende de la posición, orientación ni existencia del visor.
            panel = LabFactory.Node("BibliotecaCientifica", transform, Vector3.zero, Quaternion.identity).transform;
            if (launcher != null) launcher.gameObject.SetActive(false);
            Draw();
        }

        void Draw()
        {
            LabFactory.ClearChildren(panel);
            bool overview = page == Entries.Length;
            bool table = gameObject.scene.name == LabState.TableScene;
            Vector2 size = table ? TablePanelSize : ElementPanelSize;
            var s = LabFactory.Screen("Fuente", panel, Vector3.zero, Quaternion.identity,
                size, overview ? "LÍMITES DEL MODELO" : "PARA DOCENTES · " + Entries[page].Id);
            string body = overview
                ? "Representaciones sin escala física. La nube es ilustrativa y las reacciones no son instrucciones experimentales.\n\nLas reseñas históricas heredadas requieren revisión bibliográfica individual.\n\nQuimikaze es una propuesta didáctica: todavía no demuestra eficacia escolar."
                : "<b>" + Entries[page].Citation + "</b>\n\n" + Entries[page].Application + "\n\n<size=75%>" + Entries[page].Url + "</size>";
            var text = LabFactory.Text(s, body, new Vector3(0, table ? 0.06f : 0.14f, -0.025f), table ? 0.11f : 0.095f,
                Palette.Text, TextAlignmentOptions.TopLeft, size.x - 0.4f, table ? 1.8f : 2.0f);
            text.enableAutoSizing = true;
            text.fontSizeMin = table ? 0.85f : 0.64f;
            text.fontSizeMax = table ? 1.1f : 0.95f;
            text.overflowMode = TextOverflowModes.Ellipsis;
            float navX = table ? 1.1f : 0.64f;
            float navY = table ? -1.1f : -1.3f;
            var navSize = new Vector2(table ? 0.85f : 0.56f, 0.34f);
            LabFactory.Button(s, "Anterior", new Vector3(-navX, navY, -0.03f), navSize, table ? 0.1f : 0.075f)
                .OnClick += () => { page = (page + Entries.Length) % (Entries.Length + 1); Draw(); };
            LabFactory.Button(s, "Cerrar", new Vector3(0, navY, -0.03f), navSize, table ? 0.1f : 0.075f)
                .OnClick += Close;
            LabFactory.Button(s, "Siguiente", new Vector3(navX, navY, -0.03f), navSize, table ? 0.1f : 0.075f)
                .OnClick += () => { page = (page + 1) % (Entries.Length + 1); Draw(); };
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LabQuimicaVR.EditorTools
{
    /// <summary>
    /// Construye las dos escenas del laboratorio:
    ///  1) TablaPeriodica: la sala blanca con la tabla completa al frente y pantallas a los costados.
    ///  2) ElementoDetalle: el "nivel" de cada elemento (modelo 3D, historia, propiedades y reacciones).
    /// Menu: Laboratorio VR > Construir escenas.
    /// </summary>
    public static class LabSceneBuilder
    {
        public const string TableScenePath = "Assets/Scenes/TablaPeriodica.unity";
        public const string ElementScenePath = "Assets/Scenes/ElementoDetalle.unity";

        static readonly Color Navy = new Color(0.09f, 0.14f, 0.24f);
        static readonly Color NavyDim = new Color(0.3f, 0.38f, 0.5f);

        [MenuItem("Laboratorio VR/Construir escenas", priority = 1)]
        public static void BuildAll()
        {
            LabAssets.EnsureFolders();
            LabFactory.PersistHook = LabAssets.PersistTinted;
            try
            {
                LabAssets.CreateTextures();
                LabAssets.CreateMaterials();
                LabAssets.CreateMeshes();
                ElementDatabase.Reload();
                ConfigureProject();
                BuildTableScene();
                BuildElementScene();
                EditorBuildSettings.scenes = new[]
                {
                    new EditorBuildSettingsScene(TableScenePath, true),
                    new EditorBuildSettingsScene(ElementScenePath, true)
                };
                AssetDatabase.SaveAssets();
                ShowLab();
                Debug.Log("Laboratorio VR: escenas construidas.");
            }
            finally
            {
                LabFactory.PersistHook = null;
            }
        }

        // =====================================================================
        // Proyecto
        // =====================================================================
        static void ConfigureProject()
        {
            PlayerSettings.companyName = "LabQuimicaPeru";
            PlayerSettings.productName = "Laboratorio Químico VR";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.runInBackground = true;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.labquimicaperu.laboratoriovr");
            try
            {
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
                // ARMv7 + ARM64: celulares antiguos de 32 bits y los nuevos que solo aceptan 64 bits.
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
                PlayerSettings.Android.blitType = AndroidBlitType.Auto;
                PlayerSettings.vulkanEnablePreTransform = true;
                PlayerSettings.Android.applicationEntry = AndroidApplicationEntry.Activity;
            }
            catch (System.Exception e) { Debug.LogWarning("Ajustes de Android: " + e.Message); }
            AddJoystickAxes();
            SetAndroidQualityAntiAliasing(2);
        }

        /// <summary>MSAA 2x en el nivel de calidad que usa Android (es barato en las GPU de celular).</summary>
        static void SetAndroidQualityAntiAliasing(int samples)
        {
            var asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset").FirstOrDefault();
            if (asset == null) return;
            var so = new SerializedObject(asset);
            var levels = so.FindProperty("m_QualitySettings");
            var perPlatform = so.FindProperty("m_PerPlatformDefaultQuality");
            int androidLevel = -1;
            if (perPlatform != null)
            {
                for (int i = 0; i < perPlatform.arraySize; i++)
                {
                    var pair = perPlatform.GetArrayElementAtIndex(i);
                    if (pair.FindPropertyRelative("first").stringValue == "Android")
                        androidLevel = pair.FindPropertyRelative("second").intValue;
                }
            }
            if (levels == null) return;
            if (androidLevel < 0)
                for (int i = 0; i < levels.arraySize; i++)
                    if (levels.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == "Medium") androidLevel = i;
            if (androidLevel < 0 || androidLevel >= levels.arraySize) return;
            var level = levels.GetArrayElementAtIndex(androidLevel);
            level.FindPropertyRelative("antiAliasing").intValue = samples;
            so.ApplyModifiedProperties();
        }

        /// <summary>Marca como estaticos (para agruparlos al dibujar) los objetos que nunca se mueven.</summary>
        static void MarkStatic(Transform root)
        {
            if (root == null) return;
            var go = root.gameObject;
            if (go.GetComponent<TMP_Text>() != null) return;
            if (go.GetComponent<ElementTile>() || go.GetComponent<AtomModel>() || go.GetComponent<Spin>() ||
                go.GetComponent<FaceCamera>() || go.GetComponent<LegendItem>() || go.GetComponent<GazeButton>()) return;
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
            foreach (Transform child in root) MarkStatic(child);
        }

        static void AddJoystickAxes()
        {
            var asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/InputManager.asset").FirstOrDefault();
            if (asset == null) return;
            var so = new SerializedObject(asset);
            var axes = so.FindProperty("m_Axes");
            for (int a = 3; a <= 7; a++)
            {
                string name = "Joy" + a;
                bool exists = false;
                for (int i = 0; i < axes.arraySize; i++)
                    if (axes.GetArrayElementAtIndex(i).FindPropertyRelative("m_Name").stringValue == name) exists = true;
                if (exists) continue;
                axes.arraySize++;
                var p = axes.GetArrayElementAtIndex(axes.arraySize - 1);
                p.FindPropertyRelative("m_Name").stringValue = name;
                p.FindPropertyRelative("descriptiveName").stringValue = "Eje " + a + " del mando";
                p.FindPropertyRelative("descriptiveNegativeName").stringValue = "";
                p.FindPropertyRelative("negativeButton").stringValue = "";
                p.FindPropertyRelative("positiveButton").stringValue = "";
                p.FindPropertyRelative("altNegativeButton").stringValue = "";
                p.FindPropertyRelative("altPositiveButton").stringValue = "";
                p.FindPropertyRelative("gravity").floatValue = 0f;
                p.FindPropertyRelative("dead").floatValue = 0.19f;
                p.FindPropertyRelative("sensitivity").floatValue = 1f;
                p.FindPropertyRelative("snap").boolValue = false;
                p.FindPropertyRelative("invert").boolValue = false;
                p.FindPropertyRelative("type").intValue = 2;
                p.FindPropertyRelative("axis").intValue = a - 1;
                p.FindPropertyRelative("joyNum").intValue = 0;
            }
            so.ApplyModifiedProperties();
        }

        /// <summary>Abre la escena principal y encuadra la vista para ver todo el laboratorio.</summary>
        [MenuItem("Laboratorio VR/Mostrar laboratorio", priority = 2)]
        public static void ShowLab()
        {
            if (EditorApplication.isPlaying) return;
            EditorSceneManager.OpenScene(TableScenePath);
            var sv = SceneView.lastActiveSceneView;
            if (sv == null) sv = EditorWindow.GetWindow<SceneView>();
            sv.orthographic = false;
            sv.LookAtDirect(new Vector3(0, 3.2f, 4f), Quaternion.Euler(18f, 0f, 0f), 11f);
            sv.Repaint();
            var table = GameObject.Find("TablaPeriodica");
            if (table != null) Selection.activeGameObject = table;
        }

        // =====================================================================
        // Piezas comunes
        // =====================================================================
        static Scene NewScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.8f, 0.82f, 0.85f);
            RenderSettings.fog = false;
            return scene;
        }

        /// <summary>La "caja" del laboratorio: piso, techo y cuatro paredes con colision.</summary>
        static Transform Room(float halfWidth, float zMin, float zMax, float height)
        {
            var room = new GameObject("Laboratorio").transform;
            float depth = zMax - zMin, cz = (zMin + zMax) / 2f, t = 0.2f;
            LabFactory.Box("Piso", room, new Vector3(0, -t / 2, cz), new Vector3(halfWidth * 2 + 2 * t, t, depth + 2 * t), FloorMaterial(halfWidth * 2, depth), true);
            LabFactory.Box("Techo", room, new Vector3(0, height + t / 2, cz), new Vector3(halfWidth * 2 + 2 * t, t, depth + 2 * t), LabFactory.Mat("Ceiling"), true);
            LabFactory.Box("ParedFrente", room, new Vector3(0, height / 2, zMax + t / 2), new Vector3(halfWidth * 2, height, t), LabFactory.Mat("Wall"), true);
            LabFactory.Box("ParedAtras", room, new Vector3(0, height / 2, zMin - t / 2), new Vector3(halfWidth * 2, height, t), LabFactory.Mat("Wall"), true);
            LabFactory.Box("ParedIzquierda", room, new Vector3(-halfWidth - t / 2, height / 2, cz), new Vector3(t, height, depth), LabFactory.Mat("Wall"), true);
            LabFactory.Box("ParedDerecha", room, new Vector3(halfWidth + t / 2, height / 2, cz), new Vector3(t, height, depth), LabFactory.Mat("Wall"), true);

            // Zocalos y franjas de luz
            var trims = LabFactory.Node("Detalles", room).transform;
            foreach (var y in new[] { 0.06f, height - 0.35f })
            {
                var mat = y < 1 ? LabFactory.Mat("TrimSoft") : LabFactory.Mat("Trim");
                float h = y < 1 ? 0.12f : 0.04f;
                LabFactory.Box("FranjaFrente", trims, new Vector3(0, y, zMax - 0.01f), new Vector3(halfWidth * 2, h, 0.03f), mat);
                LabFactory.Box("FranjaAtras", trims, new Vector3(0, y, zMin + 0.01f), new Vector3(halfWidth * 2, h, 0.03f), mat);
                LabFactory.Box("FranjaIzq", trims, new Vector3(-halfWidth + 0.01f, y, cz), new Vector3(0.03f, h, depth), mat);
                LabFactory.Box("FranjaDer", trims, new Vector3(halfWidth - 0.01f, y, cz), new Vector3(0.03f, h, depth), mat);
            }
            // Columnas en las esquinas
            foreach (var x in new[] { -halfWidth + 0.15f, halfWidth - 0.15f })
                foreach (var z in new[] { zMin + 0.15f, zMax - 0.15f })
                    LabFactory.Box("Columna", trims, new Vector3(x, height / 2, z), new Vector3(0.3f, height, 0.3f), LabFactory.Mat("TrimSoft"));

            // Paneles de luz en el techo
            var lights = LabFactory.Node("LucesTecho", room).transform;
            int nx = Mathf.Max(2, Mathf.RoundToInt(halfWidth * 2 / 4.5f));
            int nz = Mathf.Max(2, Mathf.RoundToInt(depth / 4.5f));
            for (int i = 0; i < nx; i++)
                for (int j = 0; j < nz; j++)
                {
                    float x = -halfWidth + (i + 0.5f) * (halfWidth * 2 / nx);
                    float z = zMin + (j + 0.5f) * (depth / nz);
                    LabFactory.Box("Panel", lights, new Vector3(x, height - 0.02f, z), new Vector3(1.8f, 0.03f, 1.0f), LabFactory.Mat("LightPanel"));
                }

            foreach (Transform c in room.GetComponentsInChildren<Transform>())
                GameObjectUtility.SetStaticEditorFlags(c.gameObject, StaticEditorFlags.BatchingStatic);
            return room;
        }

        /// <summary>Material del piso con la cuadricula a escala real (1 m por cuadro grande).</summary>
        static Material FloorMaterial(float width, float depth)
        {
            string path = $"{LabAssets.MatDir}/Floor_{Mathf.RoundToInt(width)}x{Mathf.RoundToInt(depth)}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(LabFactory.Mat("Floor"));
                AssetDatabase.CreateAsset(m, path);
            }
            else
            {
                m.shader = LabFactory.Mat("Floor").shader;
                m.CopyPropertiesFromMaterial(LabFactory.Mat("Floor"));
            }
            m.mainTextureScale = new Vector2(width / 2f, depth / 2f);
            EditorUtility.SetDirty(m);
            return m;
        }

        static void Lights()
        {
            var sun = new GameObject("LuzPrincipal").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 0.55f;
            sun.color = new Color(1f, 0.98f, 0.95f);
            sun.shadows = LightShadows.None;
            sun.transform.rotation = Quaternion.Euler(55f, -30f, 0f);
            var fill = new GameObject("LuzRelleno").AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.intensity = 0.25f;
            fill.color = new Color(0.9f, 0.95f, 1f);
            fill.shadows = LightShadows.None;
            fill.transform.rotation = Quaternion.Euler(30f, 150f, 0f);
        }

        /// <summary>Zona de inicio (spawn) y el jugador VR con sus dos camaras.</summary>
        static VRRig SpawnAndRig(Vector3 position, float radius)
        {
            var spawn = LabFactory.Node("ZonaDeInicio", null, position).transform;
            LabFactory.Shape("Plataforma", PrimitiveType.Cylinder, spawn, new Vector3(0, 0.02f, 0), new Vector3(radius * 2, 0.02f, radius * 2), LabFactory.Mat("Pedestal"));
            var ring = LabFactory.LoadMesh("Anillo");
            LabFactory.MeshObject("Anillo", ring, spawn, new Vector3(0, 0.045f, 0), Vector3.one * radius, LabFactory.Tinted("GlowBase", Palette.Accent, 1.8f));
            LabFactory.MeshObject("AnilloInterior", ring, spawn, new Vector3(0, 0.045f, 0), Vector3.one * radius * 0.55f, LabFactory.Tinted("GlowBase", Palette.Accent, 0.9f));
            var label = LabFactory.Text(spawn, "INICIO", new Vector3(0, 0.05f, radius * 0.78f), 0.12f, Palette.Accent, TextAlignmentOptions.Center, 1.5f, 0.3f, true);
            label.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            var rigGo = new GameObject("JugadorVR");
            rigGo.transform.position = position + new Vector3(0, 0.08f, 0);
            var cc = rigGo.AddComponent<CharacterController>();
            cc.center = new Vector3(0, 0.9f, 0);
            cc.height = 1.8f;
            cc.radius = 0.3f;
            cc.stepOffset = 0.2f;
            var head = LabFactory.Node("Cabeza", rigGo.transform, new Vector3(0, 1.6f, 0)).transform;
            head.gameObject.AddComponent<AudioListener>();
            Camera Eye(string name, float x)
            {
                var go = LabFactory.Node(name, head, new Vector3(x, 0, 0));
                var cam = go.AddComponent<Camera>();
                cam.nearClipPlane = 0.05f;
                cam.farClipPlane = 80f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.92f, 0.94f, 0.97f);
                cam.fieldOfView = 62f;
                cam.allowHDR = false;
                return cam;
            }
            var left = Eye("OjoIzquierdo", -0.032f);
            left.gameObject.tag = "MainCamera";
            var right = Eye("OjoDerecho", 0.032f);
            right.enabled = false;

            var reticle = LabFactory.Node("Reticula", null).transform;
            var r = LabFactory.MeshObject("Aro", ring, reticle, Vector3.zero, Vector3.one, LabFactory.Mat("Reticle"));
            r.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var pointer = head.gameObject.AddComponent<GazePointer>();
            pointer.reticle = reticle;

            var rig = rigGo.AddComponent<VRRig>();
            rig.head = head;
            rig.leftEye = left;
            rig.rightEye = right;
            return rig;
        }

        /// <summary>Mesa de laboratorio con frascos, vasos y tubos de ensayo.</summary>
        static void Bench(Transform parent, Vector3 pos, float yaw, float length, int seed)
        {
            var b = LabFactory.Node("MesaLaboratorio", parent, pos, Quaternion.Euler(0, yaw, 0)).transform;
            LabFactory.Box("Mueble", b, new Vector3(0, 0.44f, 0), new Vector3(length, 0.88f, 0.85f), LabFactory.Mat("BenchBody"), true);
            LabFactory.Box("Cubierta", b, new Vector3(0, 0.9f, 0), new Vector3(length + 0.06f, 0.05f, 0.92f), LabFactory.Mat("BenchTop"));
            for (float x = -length / 2 + 0.5f; x < length / 2 - 0.2f; x += 1.0f)
                LabFactory.Box("Cajon", b, new Vector3(x, 0.62f, -0.43f), new Vector3(0.8f, 0.3f, 0.02f), LabFactory.Mat("TrimSoft"));

            var rng = new System.Random(seed);
            string[] liquids = { "LiquidBlue", "LiquidGreen", "LiquidPink", "LiquidAmber", "LiquidViolet" };
            float top = 0.925f;
            for (float x = -length / 2 + 0.35f; x < length / 2 - 0.3f; x += 0.45f + (float)rng.NextDouble() * 0.35f)
            {
                var liquid = LabFactory.Mat(liquids[rng.Next(liquids.Length)]);
                float z = (float)rng.NextDouble() * 0.3f - 0.1f;
                switch (rng.Next(4))
                {
                    case 0:
                        LabFactory.MeshObject("Erlenmeyer", LabFactory.LoadMesh("Erlenmeyer"), b, new Vector3(x, top, z), Vector3.one * 1.3f, LabFactory.Mat("Glass"));
                        LabFactory.MeshObject("Liquido", LabFactory.LoadMesh("LiquidoCono"), b, new Vector3(x, top, z), Vector3.one * 1.3f, liquid);
                        break;
                    case 1:
                        LabFactory.MeshObject("Vaso", LabFactory.LoadMesh("Vaso"), b, new Vector3(x, top, z), Vector3.one * 1.2f, LabFactory.Mat("Glass"));
                        LabFactory.MeshObject("Liquido", LabFactory.LoadMesh("LiquidoVaso"), b, new Vector3(x, top, z), Vector3.one * 1.2f, liquid);
                        break;
                    case 2:
                        LabFactory.MeshObject("Balon", LabFactory.LoadMesh("Balon"), b, new Vector3(x, top, z), Vector3.one * 1.3f, LabFactory.Mat("Glass"));
                        LabFactory.MeshObject("Liquido", LabFactory.LoadMesh("LiquidoBalon"), b, new Vector3(x, top, z), Vector3.one * 1.3f, liquid);
                        break;
                    default:
                        var rack = LabFactory.Node("Gradilla", b, new Vector3(x, top, z)).transform;
                        LabFactory.Box("Base", rack, new Vector3(0, 0.02f, 0), new Vector3(0.36f, 0.04f, 0.1f), LabFactory.Mat("BenchBody"));
                        LabFactory.Box("Soporte", rack, new Vector3(0, 0.12f, 0), new Vector3(0.36f, 0.02f, 0.1f), LabFactory.Mat("BenchBody"));
                        for (int i = 0; i < 4; i++)
                        {
                            float tx = -0.12f + i * 0.08f;
                            LabFactory.MeshObject("Tubo", LabFactory.LoadMesh("Vial"), rack, new Vector3(tx, 0.04f, 0), new Vector3(0.35f, 0.55f, 0.35f), LabFactory.Mat("Glass"));
                            LabFactory.Shape("Liquido", PrimitiveType.Cylinder, rack, new Vector3(tx, 0.08f, 0), new Vector3(0.036f, 0.04f, 0.036f), LabFactory.Mat(liquids[rng.Next(liquids.Length)]));
                        }
                        break;
                }
            }
            // Microscopio sencillo
            var m = LabFactory.Node("Microscopio", b, new Vector3(length / 2 - 0.35f, top, 0.15f)).transform;
            LabFactory.Box("Base", m, new Vector3(0, 0.02f, 0), new Vector3(0.18f, 0.04f, 0.24f), LabFactory.Mat("Metal"));
            LabFactory.Box("Brazo", m, new Vector3(0, 0.17f, 0.08f), new Vector3(0.05f, 0.3f, 0.05f), LabFactory.Mat("Metal"));
            LabFactory.Shape("Tubo", PrimitiveType.Cylinder, m, new Vector3(0, 0.27f, 0.0f), new Vector3(0.05f, 0.09f, 0.05f), LabFactory.Mat("BenchTop")).transform.localRotation = Quaternion.Euler(-25, 0, 0);
            LabFactory.Box("Platina", m, new Vector3(0, 0.12f, 0.0f), new Vector3(0.14f, 0.015f, 0.12f), LabFactory.Mat("BenchTop"));
        }

        static TextMeshPro WallText(Transform parent, string text, Vector3 pos, float size, Color color, float width = 10f, bool bold = true)
            => LabFactory.Text(parent, text, pos, size, color, TextAlignmentOptions.Center, width, size * 2.5f, bold);

        // =====================================================================
        // Escena 1: Tabla periodica
        // =====================================================================
        static void BuildTableScene()
        {
            var scene = NewScene();
            const float halfW = 13f, zMin = -6.5f, zMax = 12.2f, height = 8f;
            Room(halfW, zMin, zMax, height);
            Lights();
            SpawnAndRig(new Vector3(0, 0, 1.5f), 1.1f);

            BuildPeriodicTable(new Vector3(0, 0, zMax - 0.3f));
            var sides = LabFactory.Node("PantallasLaterales", null).transform;
            BuildBlocksScreen(sides, new Vector3(-9.6f, 3.7f, zMax - 0.12f));
            BuildPeriodsScreen(sides, new Vector3(9.6f, 3.7f, zMax - 0.12f));
            BuildLegendScreen(sides, new Vector3(-halfW + 0.12f, 3.35f, 6.5f));
            BuildGroupsScreen(sides, new Vector3(-halfW + 0.12f, 3.35f, 0.4f));
            BuildControlsScreen(sides, new Vector3(halfW - 0.12f, 3.35f, 6.5f));
            BuildFactsScreen(sides, new Vector3(halfW - 0.12f, 3.35f, 0.4f));
            BuildBackWall(sides, zMin);

            var benches = LabFactory.Node("Mesas", null).transform;
            Bench(benches, new Vector3(-halfW + 0.6f, 0, 3.5f), 90f, 9f, 1);
            Bench(benches, new Vector3(halfW - 0.6f, 0, 3.5f), -90f, 9f, 2);
            Bench(benches, new Vector3(-8.5f, 0, zMin + 0.6f), 180f, 5f, 3);
            Bench(benches, new Vector3(8.5f, 0, zMin + 0.6f), 180f, 5f, 4);

            MarkStatic(benches);
            MarkStatic(sides);
            var tableRoot = GameObject.Find("TablaPeriodica");
            if (tableRoot != null)
                foreach (Transform child in tableRoot.transform)
                    if (child.name != "Elementos" && child.name != "VistaPrevia") MarkStatic(child);
            var spawn = GameObject.Find("ZonaDeInicio");
            if (spawn != null) MarkStatic(spawn.transform);

            EditorSceneManager.SaveScene(scene, TableScenePath);
        }

        static void BuildPeriodicTable(Vector3 origin)
        {
            var root = LabFactory.Node("TablaPeriodica", null, origin).transform;
            var view = root.gameObject.AddComponent<PeriodicTableView>();
            float pitch = PeriodicTableView.Pitch, tile = PeriodicTableView.TileSize;

            LabFactory.Screen("PantallaGigante", root, new Vector3(0, 4.05f, 0.1f), Quaternion.identity, new Vector2(12.2f, 7.1f),
                "TABLA PERIÓDICA DE LOS ELEMENTOS");

            var tilesRoot = LabFactory.Node("Elementos", root).transform;
            var dark = new Color(0.1f, 0.12f, 0.15f);
            foreach (var e in ElementDatabase.All)
            {
                var pos = PeriodicTableView.Cell(e.TableRow, e.TableCol);
                var go = LabFactory.Node($"{e.Z:000}_{e.Symbol}", tilesRoot, pos + new Vector3(0, 0, -0.06f));
                var col = go.AddComponent<BoxCollider>();
                col.size = new Vector3(tile, tile, 0.1f);
                var mat = LabFactory.Mat("Cat_" + CategoryInfo.Code(e.Category));
                var body = LabFactory.Box("Cuerpo", go.transform, Vector3.zero, new Vector3(tile, tile, 0.06f), mat);
                var outline = LabFactory.Box("Contorno", go.transform, new Vector3(0, 0, 0.025f), new Vector3(tile + 0.07f, tile + 0.07f, 0.03f), LabFactory.Mat("TileHover"));
                outline.SetActive(false);
                // Un solo texto por casilla (numero, simbolo y nombre): tres veces menos objetos que dibujar.
                int nameSize = Mathf.Min(30, Mathf.FloorToInt(330f / Mathf.Max(1, e.Name.Length)));
                var label = LabFactory.Text(go.transform,
                    $"<align=left><size=34%><b>{e.Z}</b></size></align>\n<b>{e.Symbol}</b>\n<size={nameSize}%>{e.Name}</size>",
                    new Vector3(0, 0.005f, -0.035f), 0.2f, dark, TextAlignmentOptions.Center, tile - 0.05f, tile - 0.02f);
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.lineSpacing = -18f;
                var t = go.AddComponent<ElementTile>();
                t.atomicNumber = e.Z;
                t.body = body.GetComponent<Renderer>();
                t.outline = outline;
                t.normalMaterial = mat;
                t.labels = new[] { label };
                view.tiles.Add(t);
            }

            // Casillas de referencia del bloque f
            void Placeholder(int row, string text, ElementCategory cat)
            {
                var p = PeriodicTableView.Cell(row, 3) + new Vector3(0, 0, -0.06f);
                LabFactory.Box("Ref_" + text, root, p, new Vector3(tile, tile, 0.05f), LabFactory.Mat("Cat_" + CategoryInfo.Code(cat)));
                LabFactory.Text(root, text, p + new Vector3(0, 0, -0.035f), 0.085f, dark, TextAlignmentOptions.Center, tile, 0.2f, true);
            }
            Placeholder(6, "57-71", ElementCategory.Lanthanide);
            Placeholder(7, "89-103", ElementCategory.Actinide);

            // Numeros de grupo y periodo
            int[] topRow = { 0, 1, 2, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 2, 2, 2, 2, 2, 1 };
            for (int g = 1; g <= 18; g++)
            {
                var p = PeriodicTableView.Cell(topRow[g], g) + new Vector3(0, tile / 2 + 0.13f, -0.03f);
                LabFactory.Text(root, $"<b>{g}</b> <size=75%><color=#7FD6F0>{PeriodicLayout.RomanGroup(g)}</color></size>", p, 0.085f, Palette.Text,
                    TextAlignmentOptions.Center, pitch, 0.15f);
            }
            for (int r = 1; r <= 7; r++)
            {
                var p = PeriodicTableView.Cell(r, 1) + new Vector3(-tile / 2 - 0.16f, 0, -0.03f);
                LabFactory.Text(root, r.ToString(), p, 0.13f, Palette.Accent, TextAlignmentOptions.Center, 0.3f, 0.3f, true);
            }
            LabFactory.Text(root, "Periodo", PeriodicTableView.Cell(1, 1) + new Vector3(-tile / 2 - 0.16f, 0.36f, -0.03f), 0.05f, Palette.TextDim, TextAlignmentOptions.Center, 0.5f, 0.1f);
            LabFactory.Text(root, "Lantánidos", PeriodicTableView.Cell(9, 1) + new Vector3(0.02f, 0, -0.03f), 0.085f, CategoryInfo.Color(ElementCategory.Lanthanide), TextAlignmentOptions.Right, 1.6f, 0.2f, true);
            LabFactory.Text(root, "Actínidos", PeriodicTableView.Cell(10, 1) + new Vector3(0.02f, 0, -0.03f), 0.085f, CategoryInfo.Color(ElementCategory.Actinide), TextAlignmentOptions.Right, 1.6f, 0.2f, true);

            // Vista previa en el hueco superior (columnas 3 a 12, filas 1 a 3)
            var a = PeriodicTableView.Cell(1, 3);
            var b = PeriodicTableView.Cell(4, 12);
            float topY = a.y + tile / 2f, bottomY = b.y + tile / 2f + 0.55f;
            var center = new Vector3((a.x + b.x) / 2f, (topY + bottomY) / 2f, -0.02f);
            var size = new Vector2(b.x - a.x + tile, topY - bottomY);
            var preview = LabFactory.Node("VistaPrevia", root, center).transform;
            LabFactory.Card(preview, Vector3.zero, size, new Color(0.11f, 0.19f, 0.3f));
            LabFactory.Box("Borde", preview, new Vector3(0, 0, 0.004f), new Vector3(size.x + 0.04f, size.y + 0.04f, 0.004f), LabFactory.Tinted("GlowBase", Palette.Accent, 1.2f));

            var hint = LabFactory.Node("Ayuda", preview).transform;
            LabFactory.Text(hint, "<b>Mira un elemento</b> y pulsa el <color=#FFC34D>botón del visor</color> para entrar a su laboratorio",
                new Vector3(0, 0.2f, -0.02f), 0.2f, Color.white, TextAlignmentOptions.Center, size.x - 0.3f, 0.75f);
            LabFactory.Text(hint, "Usa el joystick izquierdo del mando para acercarte a la tabla",
                new Vector3(0, -0.48f, -0.02f), 0.12f, Palette.TextDim, TextAlignmentOptions.Center, size.x - 0.3f, 0.3f);

            var content = LabFactory.Node("Ficha", preview).transform;
            float tileX = -size.x / 2 + 0.8f;
            var ptile = LabFactory.Box("Casilla", content, new Vector3(tileX, 0, -0.03f), new Vector3(1.3f, 1.3f, 0.05f), LabFactory.Mat("Cat_NM"));
            view.previewTile = ptile.GetComponent<Renderer>();
            view.previewNumber = LabFactory.Text(content, "6", new Vector3(tileX, 0.48f, -0.06f), 0.13f, dark, TextAlignmentOptions.TopLeft, 1.15f, 0.22f, true);
            view.previewSymbol = LabFactory.Text(content, "C", new Vector3(tileX, 0.04f, -0.06f), 0.55f, dark, TextAlignmentOptions.Center, 1.3f, 0.7f, true);
            view.previewName = LabFactory.Text(content, "Carbono", new Vector3(tileX, -0.46f, -0.06f), 0.11f, dark, TextAlignmentOptions.Center, 1.25f, 0.18f);
            float textX = tileX + 0.8f + (size.x - 1.7f) / 2f;
            view.previewTitle = LabFactory.Text(content, "Carbono", new Vector3(textX, 0.5f, -0.02f), 0.22f, Color.white, TextAlignmentOptions.Left, size.x - 1.8f, 0.35f);
            view.previewInfo = LabFactory.Text(content, "", new Vector3(textX, -0.22f, -0.02f), 0.1f, Palette.Text, TextAlignmentOptions.TopLeft, size.x - 1.8f, 0.95f);
            view.previewInfo.lineSpacing = 4f;
            view.previewHint = hint.gameObject;
            view.previewContent = content.gameObject;
            content.gameObject.SetActive(false);
        }

        static void BuildBlocksScreen(Transform parent, Vector3 pos)
        {
            var s = LabFactory.Screen("PantallaBloques", parent, pos, Quaternion.identity, new Vector2(4.4f, 4.6f), "BLOQUES s · p · d · f");
            var blockColors = new Dictionary<char, Color>
            {
                { 's', new Color(1f, 0.45f, 0.45f) }, { 'p', new Color(1f, 0.85f, 0.3f) },
                { 'd', new Color(0.35f, 0.7f, 1f) }, { 'f', new Color(0.45f, 0.9f, 0.55f) }
            };
            float cell = 0.215f;
            var grid = LabFactory.Node("MiniTabla", s, new Vector3(0, 0.5f, -0.02f)).transform;
            foreach (var e in ElementDatabase.All)
            {
                float row = e.TableRow >= 9 ? e.TableRow - 1.5f : e.TableRow - 1f;
                var p = new Vector3((e.TableCol - 9.5f) * cell, 1.0f - row * cell, 0);
                LabFactory.Box("c", grid, p, new Vector3(cell * 0.86f, cell * 0.86f, 0.01f), LabFactory.Tinted("Card", blockColors[e.Block]));
            }
            string text =
                "<color=#FF7373><b>s</b></color>  grupos 1 y 2 (y el helio)\n" +
                "<color=#FFD94D><b>p</b></color>  grupos 13 al 18\n" +
                "<color=#59B3FF><b>d</b></color>  metales de transición (3 al 12)\n" +
                "<color=#73E68C><b>f</b></color>  lantánidos y actínidos\n" +
                "<size=80%><color=#9FB4CC>El bloque indica el último subnivel que se llena de electrones.</color></size>";
            LabFactory.Text(s, text, new Vector3(0, -1.4f, -0.02f), 0.16f, Palette.Text, TextAlignmentOptions.Left, 4.0f, 1.4f);
        }

        static void BuildPeriodsScreen(Transform parent, Vector3 pos)
        {
            var s = LabFactory.Screen("PantallaPeriodos", parent, pos, Quaternion.identity, new Vector2(4.4f, 4.6f), "PERIODOS (FILAS)");
            var diagram = LabFactory.Node("Niveles", s, new Vector3(0, 0.55f, -0.05f), Quaternion.Euler(-90f, 0, 0)).transform;
            LabFactory.Shape("Nucleo", PrimitiveType.Sphere, diagram, Vector3.zero, Vector3.one * 0.16f, LabFactory.Mat("Proton"));
            for (int n = 1; n <= 7; n++)
            {
                float r = 0.18f + n * 0.165f;
                var c = Color.Lerp(new Color(0.3f, 0.85f, 1f), new Color(0.75f, 0.45f, 1f), (n - 1) / 6f);
                LabFactory.Circle("Nivel" + n, diagram, r, 0.014f, c);
            }
            LabFactory.Text(s, "Hay <b>7 periodos</b>. El número del periodo indica cuántos <color=#FFC34D>niveles de energía</color> (capas K, L, M, N, O, P, Q) tiene el átomo.\n<size=80%><color=#9FB4CC>Ejemplo: el sodio (Na) está en el periodo 3, así que tiene 3 niveles con electrones.</color></size>",
                new Vector3(0, -1.42f, -0.02f), 0.15f, Palette.Text, TextAlignmentOptions.Center, 4.1f, 1.4f);
        }

        static void BuildLegendScreen(Transform parent, Vector3 pos)
        {
            var s = LabFactory.Screen("PantallaLeyenda", parent, pos, Quaternion.Euler(0, -90f, 0), new Vector2(5.2f, 4.2f), "TIPOS DE ELEMENTOS");
            LabFactory.Text(s, "Mira un color para resaltar esos elementos en la tabla. Pulsa para dejarlo fijo.",
                new Vector3(0, 1.36f, -0.01f), 0.11f, Palette.TextDim, TextAlignmentOptions.Center, 4.9f, 0.2f);
            var cats = CategoryInfo.AllCategories;
            for (int i = 0; i < cats.Length; i++)
            {
                int col = i % 2, row = i / 2;
                var p = new Vector3(-1.27f + col * 2.54f, 0.95f - row * 0.62f, -0.02f);
                var item = LabFactory.Node("Leyenda_" + CategoryInfo.Code(cats[i]), s, p);
                var bc = item.AddComponent<BoxCollider>();
                bc.size = new Vector3(2.4f, 0.54f, 0.08f);
                LabFactory.Card(item.transform, Vector3.zero, new Vector2(2.4f, 0.54f), new Color(0.12f, 0.2f, 0.32f));
                var outline = LabFactory.Box("Contorno", item.transform, new Vector3(0, 0, 0.002f), new Vector3(2.46f, 0.6f, 0.004f), LabFactory.Mat("TileHover"));
                outline.SetActive(false);
                LabFactory.Box("Color", item.transform, new Vector3(-0.92f, 0, -0.03f), new Vector3(0.38f, 0.38f, 0.04f), LabFactory.Mat("Cat_" + CategoryInfo.Code(cats[i])));
                LabFactory.Text(item.transform, CategoryInfo.Name(cats[i]), new Vector3(0.28f, 0, -0.03f), 0.15f, Color.white, TextAlignmentOptions.Left, 1.8f, 0.45f, true);
                var li = item.AddComponent<LegendItem>();
                li.category = cats[i];
                li.outline = outline;
            }
        }

        static void BuildGroupsScreen(Transform parent, Vector3 pos)
        {
            var s = LabFactory.Screen("PantallaGrupos", parent, pos, Quaternion.Euler(0, -90f, 0), new Vector2(5.2f, 4.2f), "GRUPOS Y SUS NOMBRES");
            for (int g = 1; g <= 18; g++)
            {
                int col = (g - 1) / 9, row = (g - 1) % 9;
                string name = PeriodicLayout.GroupName(g).Replace(" (familia del cobre)", "");
                var p = new Vector3(-1.27f + col * 2.54f, 1.3f - row * 0.3f, -0.02f);
                var t = LabFactory.Text(s, $"<color=#00C8F2><b>{g}</b></color> <color=#FFC34D>{PeriodicLayout.RomanGroup(g)}</color>  {name}", p, 0.125f, Palette.Text,
                    TextAlignmentOptions.Left, 2.45f, 0.28f);
                t.textWrappingMode = TextWrappingModes.NoWrap;
                t.enableAutoSizing = true;
                t.fontSizeMax = 1.25f;
                t.fontSizeMin = 0.8f;
            }
            LabFactory.Text(s, "Los elementos de un mismo grupo tienen los mismos electrones en su último nivel, por eso se comportan de forma parecida.",
                new Vector3(0, -1.6f, -0.02f), 0.1f, Palette.TextDim, TextAlignmentOptions.Center, 4.9f, 0.4f);
        }

        static void BuildControlsScreen(Transform parent, Vector3 pos)
        {
            var s = LabFactory.Screen("PantallaControles", parent, pos, Quaternion.Euler(0, 90f, 0), new Vector2(5.2f, 4.2f), "¿CÓMO SE USA?");
            // Mando dibujado con piezas simples
            var pad = LabFactory.Node("Mando", s, new Vector3(0, 0.95f, -0.04f)).transform;
            var body = LabFactory.Tinted("Card", new Color(0.22f, 0.27f, 0.35f));
            LabFactory.Box("Cuerpo", pad, Vector3.zero, new Vector3(1.6f, 0.62f, 0.05f), body);
            LabFactory.Shape("AsaIzq", PrimitiveType.Cylinder, pad, new Vector3(-0.72f, -0.18f, 0), new Vector3(0.5f, 0.025f, 0.5f), body).transform.localRotation = Quaternion.Euler(90, 0, 0);
            LabFactory.Shape("AsaDer", PrimitiveType.Cylinder, pad, new Vector3(0.72f, -0.18f, 0), new Vector3(0.5f, 0.025f, 0.5f), body).transform.localRotation = Quaternion.Euler(90, 0, 0);
            var stick = LabFactory.Tinted("Card", new Color(0.08f, 0.1f, 0.13f));
            LabFactory.Shape("StickIzq", PrimitiveType.Cylinder, pad, new Vector3(-0.45f, 0.05f, -0.04f), new Vector3(0.2f, 0.02f, 0.2f), stick).transform.localRotation = Quaternion.Euler(90, 0, 0);
            LabFactory.Shape("StickDer", PrimitiveType.Cylinder, pad, new Vector3(0.25f, -0.15f, -0.04f), new Vector3(0.2f, 0.02f, 0.2f), stick).transform.localRotation = Quaternion.Euler(90, 0, 0);
            (string label, Vector3 p, Color c)[] buttons =
            {
                ("A", new Vector3(0.55f, -0.04f, -0.04f), new Color(0.3f, 0.8f, 0.4f)),
                ("B", new Vector3(0.7f, 0.1f, -0.04f), new Color(0.95f, 0.35f, 0.35f)),
                ("X", new Vector3(0.4f, 0.1f, -0.04f), new Color(0.3f, 0.6f, 1f)),
                ("Y", new Vector3(0.55f, 0.24f, -0.04f), new Color(1f, 0.8f, 0.2f)),
            };
            foreach (var (label, p, c) in buttons)
            {
                LabFactory.Shape("Boton" + label, PrimitiveType.Cylinder, pad, p, new Vector3(0.12f, 0.02f, 0.12f), LabFactory.Tinted("Card", c)).transform.localRotation = Quaternion.Euler(90, 0, 0);
                LabFactory.Text(pad, label, p + new Vector3(0, 0, -0.03f), 0.06f, Color.black, TextAlignmentOptions.Center, 0.12f, 0.1f, true);
            }
            string text =
                "<color=#FFC34D>MIRA</color> un elemento: se ilumina y aparece su ficha.\n" +
                "<color=#FFC34D>TOCA EL BOTÓN DEL VISOR</color> o <color=#4DCC66>A</color>: entrar / pulsar.\n" +
                "<color=#FFC34D>MANTÉN PULSADO</color> el botón del visor o <color=#FFCC33>Y</color>: recentrar la vista.\n" +
                "<color=#FFC34D>JOYSTICK IZQUIERDO</color>: caminar.   <color=#FFC34D>DERECHO</color>: girar.\n" +
                "<color=#F25959>B</color>: volver a la tabla.   <color=#4D99FF>X</color>: cambiar modelo del átomo.\n" +
                "<color=#FFC34D>L1 / R1</color>: ajustar las lentes si ves doble.";
            LabFactory.Text(s, text, new Vector3(0, -0.8f, -0.02f), 0.13f, Palette.Text, TextAlignmentOptions.Left, 4.9f, 2.0f);
        }

        static void BuildFactsScreen(Transform parent, Vector3 pos)
        {
            var s = LabFactory.Screen("PantallaDatos", parent, pos, Quaternion.Euler(0, 90f, 0), new Vector2(5.2f, 4.2f), "¿SABÍAS QUE...?", Palette.Accent2);
            var screen = s.gameObject.AddComponent<FunFactsScreen>();
            screen.text = LabFactory.Text(s, FunFactsScreen.Facts[0], new Vector3(0, 0.05f, -0.02f), 0.2f, Color.white, TextAlignmentOptions.Center, 4.6f, 2.6f);
            screen.counter = LabFactory.Text(s, "Dato 1", new Vector3(0, -1.6f, -0.02f), 0.08f, Palette.TextDim, TextAlignmentOptions.Center, 4.6f, 0.2f);
        }

        static void BuildBackWall(Transform parent, float zMin)
        {
            var wall = LabFactory.Node("ParedAtras", parent, new Vector3(0, 0, zMin + 0.12f), Quaternion.Euler(0, 180f, 0)).transform;
            WallText(wall, "LABORATORIO VIRTUAL DE QUÍMICA", new Vector3(0, 6.75f, 0), 0.5f, Navy, 16f);
            WallText(wall, "Explora los 118 elementos de la tabla periódica", new Vector3(0, 6.05f, 0), 0.22f, NavyDim, 16f, false);
            LabFactory.Box("LineaTitulo", wall, new Vector3(0, 5.7f, 0.02f), new Vector3(9f, 0.04f, 0.02f), LabFactory.Mat("Trim"));

            var numbers = LabFactory.Screen("PantallaNumeros", wall, new Vector3(-5.2f, 3.2f, 0), Quaternion.identity, new Vector2(4.6f, 3.4f), "LA TABLA EN NÚMEROS");
            LabFactory.Text(numbers,
                "<color=#FFC34D><b>118</b></color> elementos conocidos\n" +
                "<color=#FFC34D><b>94</b></color> existen en la naturaleza\n" +
                "<color=#FFC34D><b>11</b></color> gases y <color=#FFC34D><b>2</b></color> líquidos a 25 °C\n" +
                "<color=#FFC34D><b>7</b></color> periodos y <color=#FFC34D><b>18</b></color> grupos\n" +
                "Más ligero: <b>hidrógeno</b> · Más denso: <b>osmio</b>\n" +
                "Último descubierto: <b>teneso</b> (2010)",
                new Vector3(0, -0.25f, -0.02f), 0.19f, Palette.Text, TextAlignmentOptions.Left, 4.3f, 2.6f);

            var atomInfo = LabFactory.Screen("PantallaAtomo", wall, new Vector3(5.2f, 3.2f, 0), Quaternion.identity, new Vector2(4.6f, 3.4f), "¿QUÉ ES UN ÁTOMO?");
            LabFactory.Text(atomInfo,
                "Es la parte más pequeña de un elemento.\n\n" +
                "En el centro tiene un <color=#FF6B6B><b>núcleo</b></color> con <color=#FF6B6B>protones (+)</color> y <color=#B0BEC5>neutrones</color>. Alrededor se mueven los <color=#4FC3F7>electrones (-)</color>.\n\n" +
                "El número de protones (<b>número atómico Z</b>) decide qué elemento es.",
                new Vector3(0, -0.25f, -0.02f), 0.165f, Palette.Text, TextAlignmentOptions.Left, 4.3f, 2.6f);

            // Atomo decorativo girando entre las dos pantallas
            var holder = LabFactory.Node("AtomoDecorativo", wall, new Vector3(0, 2.35f, -0.9f)).transform;
            var atom = LabFactory.Node("Atomo", holder).AddComponent<AtomModel>();
            atom.Build(ElementDatabase.Get(6));
            holder.localScale = Vector3.one * 1.3f;
            LabFactory.Shape("Soporte", PrimitiveType.Cylinder, wall, new Vector3(0, 0.6f, -0.9f), new Vector3(0.9f, 0.6f, 0.9f), LabFactory.Mat("Pedestal"), true);
            LabFactory.Shape("Aro", PrimitiveType.Cylinder, wall, new Vector3(0, 1.21f, -0.9f), new Vector3(0.95f, 0.012f, 0.95f), LabFactory.Tinted("GlowBase", Palette.Accent, 1.6f));
            LabFactory.MeshObject("Haz", LabFactory.LoadMesh("Haz"), wall, new Vector3(0, 1.22f, -0.9f), new Vector3(0.75f, 0.55f, 0.75f), LabFactory.Mat("Hologram"));
        }

        // =====================================================================
        // Escena 2: Laboratorio de un elemento
        // =====================================================================
        static void BuildElementScene()
        {
            var scene = NewScene();
            const float halfW = 5f, zMin = -4f, zMax = 10f, height = 5.6f;
            Room(halfW, zMin, zMax, height);
            Lights();
            SpawnAndRig(Vector3.zero, 0.9f);

            var content = new GameObject("Contenido").transform;
            var controller = content.gameObject.AddComponent<ElementDetailController>();

            // Pantalla grande del frente (detras del modelo)
            LabFactory.Screen("PantallaPrincipal", content, new Vector3(0, 3.0f, zMax - 0.12f), Quaternion.identity, new Vector2(5.8f, 4.7f));
            controller.titleAnchor = LabFactory.Node("Titulo", content, new Vector3(0, 4.72f, zMax - 0.16f)).transform;

            // Pedestal holografico
            var ped = LabFactory.Node("Pedestal", content, new Vector3(0, 0, 6.0f)).transform;
            LabFactory.Shape("Base", PrimitiveType.Cylinder, ped, new Vector3(0, 0.25f, 0), new Vector3(1.3f, 0.25f, 1.3f), LabFactory.Mat("Pedestal"), true);
            LabFactory.Shape("Aro", PrimitiveType.Cylinder, ped, new Vector3(0, 0.505f, 0), new Vector3(1.36f, 0.01f, 1.36f), LabFactory.Tinted("GlowBase", Palette.Accent, 1.8f));
            LabFactory.Shape("Emisor", PrimitiveType.Cylinder, ped, new Vector3(0, 0.52f, 0), new Vector3(0.9f, 0.02f, 0.9f), LabFactory.Tinted("GlowBase", Palette.Accent, 0.8f));
            LabFactory.MeshObject("Haz", LabFactory.LoadMesh("Haz"), ped, new Vector3(0, 0.53f, 0), Vector3.one, LabFactory.Mat("Hologram"));
            controller.modelAnchor = LabFactory.Node("Modelo", content, new Vector3(0, 1.85f, 6.0f)).transform;

            controller.sampleAnchor = LabFactory.Node("Muestra", content, new Vector3(2.7f, 0, 2.9f)).transform;
            controller.historyAnchor = LabFactory.Node("Historia", content, new Vector3(-halfW + 0.12f, 2.55f, 4.0f), Quaternion.Euler(0, -90f, 0)).transform;
            controller.propsAnchor = LabFactory.Node("Propiedades", content, new Vector3(halfW - 0.12f, 2.55f, 4.0f), Quaternion.Euler(0, 90f, 0)).transform;
            controller.reactionAnchor = LabFactory.Node("Reacciones", content, new Vector3(-3.35f, 2.2f, 8.3f), Quaternion.Euler(0, -24f, 0)).transform;
            controller.controlsAnchor = LabFactory.Node("Controles", content, new Vector3(3.45f, 1.95f, 8.3f), Quaternion.Euler(0, 24f, 0)).transform;
            controller.backAnchor = LabFactory.Node("Salida", content, new Vector3(0, 2.3f, zMin + 0.12f), Quaternion.Euler(0, 180f, 0)).transform;

            // Indicacion en el piso
            var floorHint = LabFactory.Text(content, "Usa el joystick para acercarte a las pantallas  ·  Mira a los lados", new Vector3(0, 0.03f, 1.6f), 0.11f,
                new Color(0.2f, 0.45f, 0.6f), TextAlignmentOptions.Center, 5f, 0.3f, true);
            floorHint.transform.localRotation = Quaternion.Euler(90f, 0, 0);

            var benches = LabFactory.Node("Mesas", null).transform;
            Bench(benches, new Vector3(-3.3f, 0, zMin + 0.6f), 180f, 2.8f, 5);
            Bench(benches, new Vector3(3.3f, 0, zMin + 0.6f), 180f, 2.8f, 6);
            MarkStatic(benches);
            MarkStatic(ped);
            var spawn = GameObject.Find("ZonaDeInicio");
            if (spawn != null) MarkStatic(spawn.transform);

            controller.previewZ = 6;
            controller.Populate(controller.previewZ);
            EditorSceneManager.SaveScene(scene, ElementScenePath);
        }

        /// <summary>Muestra otro elemento en la escena de detalle (solo en el editor).</summary>
        [MenuItem("Laboratorio VR/Vista previa: siguiente elemento", priority = 20)]
        public static void PreviewNext()
        {
            var c = Object.FindFirstObjectByType<ElementDetailController>();
            if (c == null) { Debug.LogWarning("Abre la escena ElementoDetalle primero."); return; }
            LabFactory.PersistHook = LabAssets.PersistTinted;
            c.previewZ = c.previewZ % 118 + 1;
            c.Populate(c.previewZ);
            LabFactory.PersistHook = null;
            EditorSceneManager.MarkSceneDirty(c.gameObject.scene);
        }
    }
}

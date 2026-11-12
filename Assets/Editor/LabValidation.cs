using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LabQuimicaVR.EditorTools
{
    /// <summary>Pruebas con el importador, tipos, recursos y geometría reales de Unity.</summary>
    public static class LabValidation
    {
        const string DirectoryPath = "AutoSync/validacion";
        [Serializable]
        public class Result
        {
            public bool success;
            public string startedUtc, finishedUtc, error, scope = "Unity Editor: lógica, sockets TCP localhost, recursos y construcción de las 118 vistas";
            public int contentChecks, educationChecks, elementsBuilt, textOverflowCount;
            public string[] lanGroups;
            public string[] engineErrors;
        }

        [MenuItem("Quimikaze/Validar entrega completa", priority = 80)]
        public static void RunMenu() => Run();

        public static void RunBatch()
        {
            try { Run(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        public static void Run()
        {
            Directory.CreateDirectory(DirectoryPath);
            var result = new Result { startedUtc = DateTime.UtcNow.ToString("o") };
            var errors = new List<string>();
            Application.LogCallback onLog = (message, stack, type) =>
            {
                if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                    errors.Add(message + "\n" + stack);
            };
            Application.logMessageReceived += onLog;
            try
            {
                ElementDatabase.Reload();
                ElementExtras.Reload();
                result.contentChecks = ContentInputValidation.Run();
                result.educationChecks = EducationValidation.Run();
                LanValidation.Run();
                result.lanGroups = LanValidation.Passed.ToArray();
                if (ScientificLibrary.Entries.Select(e => e.Id).Distinct().Count() != ScientificLibrary.Entries.Length ||
                    ScientificLibrary.Entries.Any(e => string.IsNullOrEmpty(e.Citation) || !e.Url.StartsWith("https://")))
                    throw new Exception("Biblioteca de fuentes incompleta.");
                LabSceneBuilder.BuildAll();
                var table = Object.FindAnyObjectByType<PeriodicTableView>();
                if (table == null || table.tiles.Count != 118) throw new Exception("La escena no contiene las 118 casillas.");
                CheckRig();
                EditorSceneManager.OpenScene(LabSceneBuilder.ElementScenePath);
                CheckRig();
                var controller = Object.FindAnyObjectByType<ElementDetailController>();
                if (controller == null || controller.challengeAnchor == null) throw new Exception("Falta estación de retos.");
                var overflow = new List<string> { "Z\tjerarquia\ttexto" };
                for (int z = 1; z <= 118; z++)
                {
                    controller.Populate(z);
                    foreach (var text in controller.GetComponentsInChildren<TMP_Text>(false))
                    {
                        text.ForceMeshUpdate(true, true);
                        if (text.isTextOverflowing)
                        {
                            overflow.Add(z + "\t" + PathOf(text.transform) + "\t" + text.text.Replace('\n', ' ').Replace('\t', ' '));
                            result.textOverflowCount++;
                        }
                    }
                    result.elementsBuilt++;
                }
                File.WriteAllLines(DirectoryPath + "/text_overflow.tsv", overflow);
                controller.Populate(controller.previewZ);
                LabCapture.CaptureAll();
                if (errors.Count > 0) throw new Exception("Unity registró errores: ver engineErrors en resultado.json.");
                if (result.textOverflowCount > 0) throw new Exception("Hay " + result.textOverflowCount + " textos desbordados: revisar text_overflow.tsv y las capturas antes de aprobar.");
                result.success = true;
                Debug.Log("[QA] " + result.contentChecks + " comprobaciones contenido + " + result.educationChecks +
                    " educativas; LAN " + result.lanGroups.Length + " grupos; 118 vistas construidas; desbordes texto: " + result.textOverflowCount);
            }
            catch (Exception e) { result.error = e.ToString(); throw; }
            finally
            {
                Application.logMessageReceived -= onLog;
                result.engineErrors = errors.ToArray();
                result.finishedUtc = DateTime.UtcNow.ToString("o");
                File.WriteAllText(DirectoryPath + "/resultado.json", JsonUtility.ToJson(result, true));
            }
        }

        static void CheckRig()
        {
            var rig = Object.FindAnyObjectByType<VRRig>();
            if (rig == null || rig.head == null || rig.leftEye == null || rig.rightEye == null ||
                rig.GetComponent<CharacterController>() == null || rig.head.GetComponent<GazePointer>() == null)
                throw new Exception("Jugador, cámara, colisión o puntero sin configurar.");
        }

        static string PathOf(Transform transform)
        {
            var value = transform.name;
            while (transform.parent != null) { transform = transform.parent; value = transform.name + "/" + value; }
            return value;
        }
    }
}

using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LabQuimicaVR.EditorTools
{
    /// <summary>
    /// Compila el APK del prototipo en Builds/LaboratorioQuimicaVR.apk.
    /// Desde Unity: menu Laboratorio VR > Compilar APK (Android).
    /// Desde consola: Unity.exe -batchmode -quit -projectPath ... -buildTarget Android
    ///                -executeMethod LabQuimicaVR.EditorTools.LabBuild.BuildApkBatch
    /// </summary>
    public static class LabBuild
    {
        public const string ApkPath = "Builds/LaboratorioQuimicaVR.apk";
        const string ResultFile = "AutoSync/build_result.txt";

        [MenuItem("Laboratorio VR/Compilar APK (Android)", priority = 60)]
        public static void BuildApkMenu() => BuildApk();

        /// <summary>Punto de entrada para la linea de comandos: sale con codigo 0 si el APK se creo.</summary>
        public static void BuildApkBatch()
        {
            bool ok = false;
            try { ok = BuildApk(); }
            catch (Exception e) { Write("ERROR: " + e); }
            EditorApplication.Exit(ok ? 0 : 1);
        }

        public static bool BuildApk()
        {
            Directory.CreateDirectory("AutoSync");
            Directory.CreateDirectory(Path.GetDirectoryName(ApkPath));
            // Resultado limpio en cada intento, y el APK anterior se aparta para no confundirlo con uno nuevo.
            File.WriteAllText(ResultFile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] INICIADO\n");
            if (File.Exists(ApkPath)) File.Copy(ApkPath, ApkPath + ".anterior", true);
            if (File.Exists(ApkPath)) File.Delete(ApkPath);

            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            {
                Write("ERROR: falta instalar Android Build Support en Unity Hub.");
                return false;
            }
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android &&
                !EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
            {
                Write("ERROR: no se pudo cambiar la plataforma a Android.");
                return false;
            }

            ConfigureAndroid();

            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled && File.Exists(s.path)).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
            {
                scenes = new[] { LabSceneBuilder.TableScenePath, LabSceneBuilder.ElementScenePath };
            }

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = ApkPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(options);
            var s = report.summary;
            bool ok = s.result == BuildResult.Succeeded;
            // El tamano real es el del archivo .apk (report.summary.totalSize cuenta archivos intermedios).
            float apkMb = File.Exists(ApkPath) ? new FileInfo(ApkPath).Length / (1024f * 1024f) : 0f;
            string msg = ok
                ? $"OK: APK creado en {Path.GetFullPath(ApkPath)} ({apkMb:0.0} MB) en {s.totalTime.TotalMinutes:0.0} min"
                : $"ERROR: compilación {s.result} con {s.totalErrors} errores.";
            if (!ok)
            {
                foreach (var step in report.steps)
                    foreach (var m in step.messages.Where(m => m.type == LogType.Error || m.type == LogType.Exception))
                        msg += "\n  " + m.content;
            }
            Write(msg);
            return ok;
        }

        /// <summary>Ajustes para celulares Android de gama media/baja dentro de un visor.</summary>
        static void ConfigureAndroid()
        {
            var android = NamedBuildTarget.Android;
            PlayerSettings.SetApplicationIdentifier(android, "com.labquimicaperu.laboratoriovr");
            PlayerSettings.productName = "Laboratorio Químico VR";
            PlayerSettings.companyName = "LabQuimicaPeru";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.Android.bundleVersionCode = 1;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
            // ARMv7 + ARM64: funciona en celulares antiguos de 32 bits y en los nuevos que solo aceptan 64 bits.
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
            // Actividad clasica: el Input Manager recibe los ejes de los mandos Bluetooth genericos en el orden
            // conocido (joystick derecho = ejes 3/4, cruceta = ejes 5/6), que es lo que usa LabInput.
            PlayerSettings.Android.applicationEntry = AndroidApplicationEntry.Activity;
            PlayerSettings.Android.blitType = AndroidBlitType.Auto;
            PlayerSettings.vulkanEnablePreTransform = true;
            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.development = false;
            AssetDatabase.SaveAssets();
        }

        /// <summary>Guarda todo y cierra el editor (para poder compilar por linea de comandos).</summary>
        [MenuItem("Laboratorio VR/Guardar y cerrar Unity", priority = 61)]
        public static void SaveAndQuit()
        {
            // Solo guarda escenas que ya tienen archivo: nunca abre el dialogo "Guardar escena".
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (scene.isDirty && !string.IsNullOrEmpty(scene.path)) EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
            LabAutoSync.WriteStatus("OK: guardado, cerrando Unity");
            EditorApplication.Exit(0);
        }

        static void Write(string msg)
        {
            Debug.Log("[LabBuild] " + msg);
            File.AppendAllText(ResultFile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {msg}\n");
        }
    }
}

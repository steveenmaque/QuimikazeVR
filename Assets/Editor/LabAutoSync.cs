using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace LabQuimicaVR.EditorTools
{
    /// <summary>
    /// Mantiene el editor sincronizado con cambios hechos desde fuera de Unity.
    /// Escribe una orden en AutoSync/request.txt (una por linea: "refresh" o "menu:Ruta/Del/Menu")
    /// y el editor la ejecuta aunque no tenga el foco. El resultado queda en AutoSync/status.txt
    /// y los errores de compilacion en AutoSync/compile_errors.txt.
    /// </summary>
    [InitializeOnLoad]
    public static class LabAutoSync
    {
        const string SyncDir = "AutoSync";
        const string RequestFile = SyncDir + "/request.txt";
        const string StatusFile = SyncDir + "/status.txt";
        const string ErrorsFile = SyncDir + "/compile_errors.txt";
        const string StampKey = "LabAutoSync.Stamp";
        const string PendingKey = "LabAutoSync.Pending";
        const string TmpImportedKey = "LabAutoSync.TmpImportRequested";

        static double nextPoll;
        static double pendingSince;

        static LabAutoSync()
        {
            // En compilaciones por linea de comandos no se sincroniza nada.
            if (Application.isBatchMode) return;
            Directory.CreateDirectory(SyncDir);
            // Al abrir Unity (sesion nueva) se ignora la ultima orden que haya quedado escrita:
            // solo se ejecutan ordenes escritas mientras el editor esta abierto.
            if (SessionState.GetString(StampKey, "") == "" && File.Exists(RequestFile))
                SessionState.SetString(StampKey, File.GetLastWriteTimeUtc(RequestFile).Ticks.ToString());
            CompilationPipeline.compilationStarted += _ => { if (File.Exists(ErrorsFile)) File.Delete(ErrorsFile); };
            CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompiled;
            EditorApplication.update += Poll;
            EditorApplication.delayCall += EnsureTmpEssentials;
            pendingSince = EditorApplication.timeSinceStartup;
        }

        static void OnAssemblyCompiled(string assembly, CompilerMessage[] messages)
        {
            var errors = messages.Where(m => m.type == CompilerMessageType.Error).ToArray();
            if (errors.Length == 0) return;
            File.AppendAllLines(ErrorsFile, errors.Select(e => $"{e.file}({e.line},{e.column}): {e.message}"));
        }

        static void EnsureTmpEssentials()
        {
            if (AssetDatabase.IsValidFolder("Assets/TextMesh Pro")) return;
            if (SessionState.GetBool(TmpImportedKey, false)) return;
            var info = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.ugui");
            if (info == null) return;
            var pkg = Path.Combine(info.resolvedPath, "Package Resources", "TMP Essential Resources.unitypackage");
            if (!File.Exists(pkg)) return;
            SessionState.SetBool(TmpImportedKey, true);
            AssetDatabase.ImportPackage(pkg, false);
            WriteStatus("TMP Essential Resources importados");
        }

        static void Poll()
        {
            var now = EditorApplication.timeSinceStartup;
            if (now < nextPoll) return;
            nextPoll = now + 1.0;

            if (File.Exists(RequestFile))
            {
                var stamp = File.GetLastWriteTimeUtc(RequestFile).Ticks.ToString();
                if (stamp != SessionState.GetString(StampKey, ""))
                {
                    SessionState.SetString(StampKey, stamp);
                    SessionState.SetString(PendingKey, File.ReadAllText(RequestFile));
                    pendingSince = now;
                    WriteStatus("Recargando assets...");
                    AssetDatabase.Refresh();
                    return;
                }
            }

            var pending = SessionState.GetString(PendingKey, "");
            if (string.IsNullOrWhiteSpace(pending)) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode) { WriteStatus("Esperando a salir de Play para ejecutar la orden"); return; }
            if (now - pendingSince < 1.5) return;

            SessionState.SetString(PendingKey, "");
            if (File.Exists(ErrorsFile) && new FileInfo(ErrorsFile).Length > 0)
            {
                WriteStatus("ERROR: hay errores de compilacion, ver compile_errors.txt");
                return;
            }
            Execute(pending);
        }

        static void Execute(string commands)
        {
            foreach (var raw in commands.Split('\n'))
            {
                var line = raw.Trim();
                if (line.Length == 0 || line == "refresh") continue;
                if (!line.StartsWith("menu:")) continue;
                var menu = line.Substring(5).Trim();
                try
                {
                    var ok = EditorApplication.ExecuteMenuItem(menu);
                    WriteStatus(ok ? $"OK: {menu}" : $"ERROR: menu no encontrado: {menu}");
                }
                catch (Exception e)
                {
                    WriteStatus($"ERROR en {menu}: {e}");
                }
            }
            if (!commands.Contains("menu:")) WriteStatus("OK: refresh");
        }

        public static void WriteStatus(string msg)
        {
            File.AppendAllText(StatusFile, $"[{DateTime.Now:HH:mm:ss}] {msg}\n");
        }
    }
}

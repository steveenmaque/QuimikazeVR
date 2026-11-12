using System;
using System.IO;
using UnityEngine;

namespace LabQuimicaVR
{
    /// <summary>Exporta primero; compartir nunca sustituye ni elimina la copia local.</summary>
    public static class ReportShare
    {
        public static string ExportAndShare()
        {
            string path = LearningProgress.ExportCsv();
            Debug.Log("[Quimikaze] Informe docente: " + path);
            if (LanClassroom.HasActiveSession)
                return "Informe guardado. Al terminar, sal del aula LAN y pulsa Informe para compartirlo. Así evitas suspender la sesión al abrir otra aplicación.";
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unity.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var bridge = new AndroidJavaClass("com.quimikaze.reports.ReportShareBridge"))
                    bridge.CallStatic("share", activity, path);
                return "Informe guardado. Se solicitó abrir Compartir: elige una aplicación para enviar o guardar el CSV.\nNo incluye nombres; cancelar conserva la copia local.";
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Informe guardado; compartir no disponible: " + ex.Message);
                return "Informe guardado en este dispositivo. No se pudo abrir Compartir; inténtalo de nuevo. La copia local se conserva.";
            }
#else
            try
            {
#if UNITY_EDITOR
                UnityEditor.EditorUtility.RevealInFinder(path);
#else
                Application.OpenURL(new Uri(Path.GetDirectoryName(path) + Path.DirectorySeparatorChar).AbsoluteUri);
#endif
                return "Informe CSV guardado. Se solicitó abrir su carpeta para copiarlo o enviarlo. No incluye nombres.";
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Informe guardado; carpeta no disponible: " + ex.Message);
                return "Informe guardado en la carpeta Informes de este dispositivo. No se pudo abrir su carpeta automáticamente.";
            }
#endif
        }
    }
}

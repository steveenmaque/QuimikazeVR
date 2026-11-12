using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using LabQuimicaVR.EditorTools;
using UnityEngine;

string FindRoot()
{
    string path = AppContext.BaseDirectory;
    while (path != null)
    {
        if (File.Exists(Path.Combine(path, "Assets", "Resources", "Datos", "elementos_base.txt"))) return path;
        path = Directory.GetParent(path)?.FullName;
    }
    throw new DirectoryNotFoundException("Ejecuta el harness dentro del repositorio QuimikazeVR.");
}

string root = FindRoot();
Resources.Root = Path.Combine(root, "Assets", "Resources");
string suite = "all";
string output = Path.Combine(root, "AutoSync", "logic_results.json");
for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--suite" && i + 1 < args.Length) suite = args[++i];
    else if (args[i] == "--output" && i + 1 < args.Length) output = Path.GetFullPath(args[++i]);
    else throw new ArgumentException("Argumento desconocido: " + args[i]);
}
if (suite != "all" && suite != "education" && suite != "content" && suite != "lan")
    throw new ArgumentException("Suite debe ser all, education, content o lan.");

var results = new List<object>();
int failures = 0, total = 0;
void Run(string name, Func<int> action)
{
    var timer = Stopwatch.StartNew();
    try
    {
        int checks = action(); total += checks;
        results.Add(new { suite = name, passed = true, checks, elapsedMs = timer.ElapsedMilliseconds });
    }
    catch (Exception ex)
    {
        failures++;
        results.Add(new { suite = name, passed = false, elapsedMs = timer.ElapsedMilliseconds, exception = ex.ToString() });
        Console.Error.WriteLine(ex);
    }
}
if (suite == "all" || suite == "education") Run("EducationValidation", EducationValidation.Run);
if (suite == "all" || suite == "content") Run("ContentInputValidation", ContentInputValidation.Run);
if (suite == "all" || suite == "lan") Run("LanValidation", () =>
{
    LanValidation.Run();
    return (int)typeof(LanValidation).GetField("checks", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
});

var sourceFiles = new[] {
    "Assets/Scripts/Aprendizaje/LearningProgress.cs", "Assets/Scripts/Aprendizaje/LearningRoutes.cs", "Assets/Scripts/Aprendizaje/ChallengeCatalog.cs",
    "Assets/Scripts/Datos/ElementData.cs", "Assets/Scripts/Datos/ElementExtras.cs", "Assets/Scripts/Datos/ElectronConfiguration.cs", "Assets/Scripts/Datos/ReactionDatabase.cs",
    "Assets/Scripts/VR/LabInput.cs", "Assets/Scripts/Red/LanTransport.cs", "Assets/Scripts/Red/LanSession.cs",
    "Assets/Editor/EducationValidation.cs", "Assets/Editor/ContentInputValidation.cs", "Assets/Editor/LanValidation.cs"
};
var report = new
{
    harness = "QuimikazeVR LogicHarness", utc = DateTime.UtcNow, runtime = RuntimeInformation.FrameworkDescription,
    platform = RuntimeInformation.OSDescription, passed = failures == 0, checks = total, failures, suites = results,
    scope = new[] { "Fuentes C# de producción enlazadas sin copiar algoritmos", "Datos TXT reales del repositorio", "Sockets TCP reales IPv4 loopback",
        "Funciones puras de normalización e integración del eje vertical", "Persistencia lógica JSON con adaptador System.Text.Json" },
    limitations = new[] { "NO ejecuta Unity ni IL2CPP", "NO verifica gráficos, escenas, sensores ni mando físico", "NO prueba Wi-Fi/hotspot ni Android",
        "Resources de imagen verifica archivo y firma PNG/JPEG: NO decodificación/importación de textura Unity", "PlayerPrefs en memoria: NO verifica almacenamiento Android ni preferencias de Unity",
        "System.Text.Json sustituye JsonUtility: la serialización nativa Unity necesita validación adicional", "Las invariantes de contenido NO validan científicamente cada afirmación de los 118 elementos" },
    resourceReads = new { textFiles = Resources.TextReads, imageFileChecks = Resources.ImageFileChecks },
    sourceSha256 = sourceFiles.ToDictionary(f => f, f => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(root, f)))))
};
Directory.CreateDirectory(Path.GetDirectoryName(output));
string reportJson = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
File.WriteAllText(output, reportJson);
Console.WriteLine(reportJson);
Console.Error.WriteLine("Informe: " + output);
return failures == 0 ? 0 : 1;

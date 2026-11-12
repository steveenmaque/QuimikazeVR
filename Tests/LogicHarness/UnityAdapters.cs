// Adaptadores exclusivos del harness .NET. No se compilan dentro de Assets ni se distribuyen en el APK.
// No simulan render, sensores, eventos reales de Input, Android ni el serializador nativo de Unity.
using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;

namespace UnityEngine
{
    public class Object { }
    public sealed class TextAsset : Object
    {
        public readonly string text;
        public TextAsset(string value) { text = value; }
    }
    public sealed class Texture2D : Object
    {
        public string SourcePath { get; }
        public Texture2D(string sourcePath) { SourcePath = sourcePath; }
    }

    public static class Resources
    {
        public static string Root { get; set; }
        public static int TextReads, ImageFileChecks;
        public static T Load<T>(string name) where T : Object
        {
            string path = Path.Combine(Root, name.Replace('/', Path.DirectorySeparatorChar));
            if (typeof(T) == typeof(TextAsset))
            {
                if (!File.Exists(path + ".txt")) return null;
                TextReads++;
                return new TextAsset(File.ReadAllText(path + ".txt")) as T;
            }
            if (typeof(T) == typeof(Texture2D))
            {
                string file = new[] { path, path + ".jpg", path + ".jpeg", path + ".png" }.FirstOrDefault(File.Exists);
                if (file == null) return null;
                using var stream = File.OpenRead(file);
                Span<byte> header = stackalloc byte[8];
                if (stream.Read(header) < 8) return null;
                bool jpeg = header[0] == 0xff && header[1] == 0xd8 && header[2] == 0xff;
                bool png = header.SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
                if (!jpeg && !png) return null;
                ImageFileChecks++;
                return new Texture2D(file) as T;
            }
            throw new NotSupportedException("Resources.Load no adaptado: " + typeof(T).FullName);
        }
    }

    public struct Color
    {
        public float r, g, b, a;
        public Color(float red, float green, float blue, float alpha = 1f) { r = red; g = green; b = blue; a = alpha; }
        public static Color white => new Color(1, 1, 1);
        public static Color gray => new Color(0.5f, 0.5f, 0.5f);
        public static Color Lerp(Color start, Color end, float t)
        {
            t = Math.Clamp(t, 0, 1);
            return new Color(start.r + (end.r - start.r) * t, start.g + (end.g - start.g) * t,
                start.b + (end.b - start.b) * t, start.a + (end.a - start.a) * t);
        }
    }

    public static class ColorUtility
    {
        public static bool TryParseHtmlString(string text, out Color color)
        {
            color = default;
            if (text == null) return false;
            text = text.TrimStart('#');
            if (text.Length != 6 && text.Length != 8) return false;
            if (!uint.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint value)) return false;
            if (text.Length == 6) value = (value << 8) | 0xff;
            color = new Color(((value >> 24) & 0xff) / 255f, ((value >> 16) & 0xff) / 255f,
                ((value >> 8) & 0xff) / 255f, (value & 0xff) / 255f);
            return true;
        }
    }

    public static class Mathf
    {
        public static int Max(int a, int b) => Math.Max(a, b);
        public static float Max(float a, float b) => Math.Max(a, b);
        public static int Clamp(int value, int min, int max) => Math.Clamp(value, min, max);
        public static float Clamp(float value, float min, float max) => Math.Clamp(value, min, max);
        public static float Abs(float value) => Math.Abs(value);
        public static int RoundToInt(float value) => (int)Math.Round(value, MidpointRounding.ToEven);
    }

    public struct Vector2
    {
        public float x, y;
        public Vector2(float a, float b) { x = a; y = b; }
        public float sqrMagnitude => x * x + y * y;
        public float magnitude => (float)Math.Sqrt(sqrMagnitude);
        public static Vector2 zero => default;
        public static Vector2 ClampMagnitude(Vector2 value, float max)
        { float length = value.magnitude; return length <= max || length == 0 ? value : new Vector2(value.x / length * max, value.y / length * max); }
    }

    public static class Debug
    {
        public static readonly List<string> Messages = new List<string>();
        public static void Log(object message) { Messages.Add(message?.ToString() ?? ""); Console.Error.WriteLine(message); }
        public static void LogWarning(object message) => Log("WARNING: " + message);
        public static void LogError(object message) => Log("ERROR: " + message);
    }

    public static class JsonUtility
    {
        static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        { IncludeFields = true, IgnoreReadOnlyProperties = true, PropertyNameCaseInsensitive = false };
        public static string ToJson(object value) => JsonSerializer.Serialize(value, value.GetType(), Options);
        public static T FromJson<T>(string value)
        {
            try { return JsonSerializer.Deserialize<T>(value, Options); }
            catch (JsonException ex) { throw new ArgumentException("JSON inválido", ex); }
        }
    }

    public static class PlayerPrefs
    {
        static readonly ConcurrentDictionary<string, string> Values = new ConcurrentDictionary<string, string>();
        public static string GetString(string key, string fallback = "") => Values.TryGetValue(key, out string value) ? value : fallback;
        public static void SetString(string key, string value) => Values[key] = value;
        public static int GetInt(string key, int fallback = 0) => int.TryParse(GetString(key), out int value) ? value : fallback;
        public static void SetInt(string key, int value) => SetString(key, value.ToString(CultureInfo.InvariantCulture));
        public static void Save() { }
    }

    public enum RuntimePlatform { WindowsPlayer, Android }
    public enum RuntimeInitializeLoadType { SubsystemRegistration, BeforeSceneLoad, AfterSceneLoad }
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class RuntimeInitializeOnLoadMethodAttribute : Attribute
    { public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType type) { } }
    public static class Application
    {
        public static RuntimePlatform platform = RuntimePlatform.WindowsPlayer;
        public static string persistentDataPath = Path.Combine(Path.GetTempPath(), "QuimikazeLogicHarness");
    }
    public static class Time { public static int frameCount; public static float unscaledTime; }
    public enum KeyCode
    {
        Q, E, T, G, PageUp, PageDown, JoystickButton0, JoystickButton1, JoystickButton2, JoystickButton3,
        JoystickButton4, JoystickButton5, JoystickButton9, Return, KeypadEnter, Space, Escape, Backspace,
        M, R, LeftBracket, RightBracket, V, I, O
    }
    // Solo enlazado para compilar LabInput. Estas funciones no se invocan al verificar las funciones puras del eje.
    public static class Input
    {
        static Exception Unsupported() => new NotSupportedException("El harness no puede probar entrada física de Unity.");
        public static float GetAxis(string name) => throw Unsupported();
        public static bool GetKey(KeyCode key) => throw Unsupported();
        public static bool GetKeyDown(KeyCode key) => throw Unsupported();
        public static bool GetMouseButton(int index) => throw Unsupported();
        public static bool GetMouseButtonDown(int index) => throw Unsupported();
        public static bool GetMouseButtonUp(int index) => throw Unsupported();
    }
}

namespace UnityEngine.SceneManagement
{
    public struct Scene { }
    public enum LoadSceneMode { Single, Additive }
    public static class SceneManager { public static event Action<Scene, LoadSceneMode> sceneLoaded; }
}
namespace UnityEditor
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class MenuItem : Attribute
    {
        public int priority;
        public MenuItem(string path) { }
    }
}

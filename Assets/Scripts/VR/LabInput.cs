using System.Collections.Generic;
using UnityEngine;

namespace LabQuimicaVR
{
    /// <summary>
    /// Entradas del laboratorio:
    ///  - Toque corto del boton del visor (o clic), A del mando, Enter o Espacio: pulsar / seleccionar.
    ///  - Mantener pulsado el boton del visor 1.5 s, Y del mando o R: recentrar la vista.
    ///  - Joystick izquierdo, cruceta, WASD o flechas: caminar.
    ///  - Joystick derecho o Q/E: girar (y mirar arriba/abajo si el celular no tiene giroscopio).
    ///  - B, Escape o boton "atras" del celular: volver.
    ///  - X o M: cambiar el modelo del atomo.
    ///  - L1 / R1 o [ / ]: ajustar la separacion de las lentes del visor.
    /// </summary>
    public static class LabInput
    {
        const float LongPress = 1.5f;
        const float MaxTap = 0.8f;

        static readonly HashSet<string> missingAxes = new HashSet<string>();
        static int frame = -1;
        static float pressStart = -1f;
        static bool longFired, tapReleased, longPressNow;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnSceneLoad()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }

        /// <summary>Un toque que empezo en la escena anterior no debe pulsar nada en la nueva.</summary>
        static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            pressStart = -1f;
            longFired = true;
            tapReleased = false;
            longPressNow = false;
        }

        static float Axis(string name)
        {
            if (missingAxes.Contains(name)) return 0f;
            try { return Input.GetAxis(name); }
            catch (System.ArgumentException) { missingAxes.Add(name); return 0f; }
        }

        static bool Android => Application.platform == RuntimePlatform.Android;

        // En Android el joystick derecho suele ser los ejes 3/4 y la cruceta los ejes 5/6.
        // En Windows (mando tipo Xbox) el joystick derecho es 4/5 y la cruceta 6/7.
        static float RightStickX => Axis(Android ? "Joy3" : "Joy4");
        static float RightStickY => Axis(Android ? "Joy4" : "Joy5");
        static float DPadX => Axis(Android ? "Joy5" : "Joy6");
        static float DPadY => Android ? -Axis("Joy6") : Axis("Joy7");

        /// <summary>Calcula una sola vez por cuadro el estado del toque (corto o mantenido).</summary>
        static void Refresh()
        {
            if (frame == Time.frameCount) return;
            frame = Time.frameCount;
            tapReleased = false;
            longPressNow = false;
            if (Input.GetMouseButtonDown(0)) { pressStart = Time.unscaledTime; longFired = false; }
            if (pressStart >= 0f && !longFired && Input.GetMouseButton(0) && Time.unscaledTime - pressStart >= LongPress)
            {
                longFired = true;
                longPressNow = true;
            }
            if (Input.GetMouseButtonUp(0))
            {
                if (pressStart >= 0f && !longFired && Time.unscaledTime - pressStart <= MaxTap) tapReleased = true;
                pressStart = -1f;
            }
        }

        public static Vector2 Move
        {
            get
            {
                var v = new Vector2(Axis("Horizontal"), Axis("Vertical"));
                var d = new Vector2(DPadX, DPadY);
                if (d.sqrMagnitude > v.sqrMagnitude) v = d;
                if (v.magnitude < 0.15f) return Vector2.zero;
                return Vector2.ClampMagnitude(v, 1f);
            }
        }

        public static float Turn
        {
            get
            {
                float t = RightStickX;
                if (Mathf.Abs(t) < 0.25f) t = 0f;
                if (Input.GetKey(KeyCode.Q)) t -= 1f;
                if (Input.GetKey(KeyCode.E)) t += 1f;
                return Mathf.Clamp(t, -1f, 1f);
            }
        }

        /// <summary>Joystick derecho vertical (negativo = mirar arriba).</summary>
        public static float LookVertical
        {
            get
            {
                float v = RightStickY;
                return Mathf.Abs(v) < 0.25f ? 0f : v;
            }
        }

        /// <summary>
        /// Pulsar. El boton del visor llega como un toque en la pantalla: cuenta al soltarlo si fue
        /// un toque corto, para que mantenerlo pulsado sirva para recentrar.
        /// </summary>
        public static bool SelectDown
        {
            get
            {
                Refresh();
                return tapReleased ||
                       Input.GetKeyDown(KeyCode.JoystickButton0) ||
                       Input.GetKeyDown(KeyCode.Return) ||
                       Input.GetKeyDown(KeyCode.KeypadEnter) ||
                       Input.GetKeyDown(KeyCode.Space);
            }
        }

        public static bool BackDown =>
            Input.GetKeyDown(KeyCode.Escape) ||
            Input.GetKeyDown(KeyCode.JoystickButton1) ||
            Input.GetKeyDown(KeyCode.Backspace);

        public static bool ModeDown =>
            Input.GetKeyDown(KeyCode.JoystickButton2) ||
            Input.GetKeyDown(KeyCode.M);

        public static bool RecenterDown
        {
            get
            {
                Refresh();
                return longPressNow ||
                       Input.GetKeyDown(KeyCode.JoystickButton3) ||
                       Input.GetKeyDown(KeyCode.R);
            }
        }

        /// <summary>-1 / +1 al pulsar L1 / R1 (o [ / ]) para ajustar la separacion de las lentes.</summary>
        public static int LensAdjust
        {
            get
            {
                if (Input.GetKeyDown(KeyCode.JoystickButton4) || Input.GetKeyDown(KeyCode.LeftBracket)) return -1;
                if (Input.GetKeyDown(KeyCode.JoystickButton5) || Input.GetKeyDown(KeyCode.RightBracket)) return 1;
                return 0;
            }
        }

        public static bool ToggleStereoDown => Input.GetKeyDown(KeyCode.V);
    }
}

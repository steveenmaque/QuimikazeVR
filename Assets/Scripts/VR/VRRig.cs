using TMPro;
using UnityEngine;

namespace LabQuimicaVR
{
    /// <summary>
    /// Jugador en realidad virtual para visor de celular (tipo Cardboard / VR Box):
    /// pantalla dividida en dos ojos centrados en las lentes del visor, cabeza controlada por el
    /// giroscopio (o por el acelerometro si el celular no tiene giroscopio) y movimiento con un
    /// mando Bluetooth generico. En el editor se usa vista normal: clic derecho + mouse para mirar,
    /// WASD para caminar y V para alternar la vista estereo.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class VRRig : MonoBehaviour
    {
        public static VRRig Instance { get; private set; }

        public Transform head;
        public Camera leftEye;
        public Camera rightEye;

        [Header("Movimiento")]
        public float moveSpeed = 2.2f;
        public float turnSpeed = 80f;
        public float lookSpeed = 70f;

        [Header("Vista")]
        public float eyeHeight = 1.6f;
        public float ipd = 0.064f;
        public float stereoFov = 80f;
        public float monoFov = 62f;
        public float mouseSensitivity = 3f;
        [Tooltip("Distancia entre los centros de las lentes del visor en mm. Se ajusta con L1/R1 (o [ y ]) y se guarda.")]
        public float lensSeparationMm = 63f;

        public bool Stereo { get; private set; }

        const string LensKey = "LabVR.LensMm";

        // Se conservan al cambiar de escena para que la vista no "salte".
        static bool centered;
        static float savedYaw;
        static bool resolutionSet;

        CharacterController controller;
        Camera background;
        bool useGyro, useTilt;
        float yawOffset, tiltPitch, stickPitch;
        float uprightTime;
        float mouseYaw, mousePitch;
        int layoutWidth;
        TextMeshPro toast;
        GameObject toastRoot;
        float toastUntil;

        void Awake()
        {
            Instance = this;
            controller = GetComponent<CharacterController>();
            lensSeparationMm = PlayerPrefs.GetFloat(LensKey, lensSeparationMm);
        }

        void Start()
        {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Application.targetFrameRate = 60;
            ReduceResolutionOnDenseScreens();
            head.localPosition = new Vector3(0, eyeHeight, 0);

            // Sin HDR: en el celular evita una textura intermedia y una copia extra por ojo.
            leftEye.allowHDR = rightEye.allowHDR = false;

            // Camara de fondo que limpia a negro los margenes que quedan fuera de los dos ojos.
            var bgGo = new GameObject("FondoNegro");
            bgGo.transform.SetParent(head, false);
            background = bgGo.AddComponent<Camera>();
            background.cullingMask = 0;
            background.clearFlags = CameraClearFlags.SolidColor;
            background.backgroundColor = Color.black;
            background.depth = Mathf.Min(leftEye.depth, rightEye.depth) - 1;
            background.allowHDR = false;
            background.allowMSAA = false;

            bool mobile = Application.isMobilePlatform;
            useGyro = mobile && SystemInfo.supportsGyroscope;
            if (useGyro) Input.gyro.enabled = true;
            useTilt = mobile && !useGyro && SystemInfo.supportsAccelerometer;
            if (useGyro && centered) yawOffset = savedYaw;

            SetStereo(mobile);

            if (mobile && !useGyro)
                ShowToast("Tu celular no tiene giroscopio: inclina la cabeza para mirar arriba o abajo y usa el joystick derecho para girar.", 8f);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        static void ReduceResolutionOnDenseScreens()
        {
            if (!Application.isMobilePlatform || resolutionSet) return;
            resolutionSet = true;
            int w = Display.main.systemWidth, h = Display.main.systemHeight;
            // En pantallas 1080p o mas, dibujar al 75 % ahorra mucho trabajo a la GPU (se dibuja dos veces).
            if (Mathf.Min(w, h) > 900)
                Screen.SetResolution(Mathf.RoundToInt(w * 0.75f), Mathf.RoundToInt(h * 0.75f), true);
        }

        public void SetStereo(bool stereo)
        {
            Stereo = stereo;
            if (background != null) background.enabled = stereo;
            if (stereo)
            {
                leftEye.transform.localPosition = new Vector3(-ipd / 2, 0, 0);
                rightEye.transform.localPosition = new Vector3(ipd / 2, 0, 0);
                leftEye.fieldOfView = rightEye.fieldOfView = stereoFov;
                rightEye.enabled = true;
                LayoutEyes();
            }
            else
            {
                leftEye.rect = new Rect(0, 0, 1, 1);
                leftEye.transform.localPosition = Vector3.zero;
                leftEye.fieldOfView = monoFov;
                rightEye.enabled = false;
            }
        }

        /// <summary>
        /// Coloca la imagen de cada ojo centrada en su lente. Las lentes estan a lensSeparationMm;
        /// si la pantalla es mas ancha, cada ojo se angosta y los bordes exteriores quedan en negro.
        /// </summary>
        void LayoutEyes()
        {
            float W = Screen.width;
            layoutWidth = Screen.width;
            float dpi = PhysicalDpi();
            // La separacion guardada no puede superar lo que la pantalla puede mostrar.
            lensSeparationMm = ClampLensToScreen(lensSeparationMm, dpi);
            float sepPx = lensSeparationMm / 25.4f * dpi;
            sepPx = Mathf.Clamp(sepPx, W * 0.3f, W * 0.7f);
            float eyeW = Mathf.Clamp(Mathf.Min(sepPx, W - sepPx), W * 0.3f, W * 0.5f);
            float cL = W * 0.5f - sepPx * 0.5f;
            float cR = W * 0.5f + sepPx * 0.5f;
            leftEye.rect = new Rect((cL - eyeW * 0.5f) / W, 0, eyeW / W, 1);
            rightEye.rect = new Rect((cR - eyeW * 0.5f) / W, 0, eyeW / W, 1);
        }

        static float ClampLensToScreen(float mm, float dpi)
        {
            float mmPerPx = 25.4f / Mathf.Max(1f, dpi);
            float minMm = Mathf.Max(50f, Screen.width * 0.3f * mmPerPx);
            float maxMm = Mathf.Min(80f, Screen.width * 0.7f * mmPerPx);
            return Mathf.Clamp(mm, minMm, Mathf.Max(minMm, maxMm));
        }

        /// <summary>Puntos por pulgada reales de la pantalla, ajustados a la resolucion de dibujo.</summary>
        static float PhysicalDpi()
        {
            float dpi = 0f;
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unity.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var res = activity.Call<AndroidJavaObject>("getResources"))
                using (var dm = res.Call<AndroidJavaObject>("getDisplayMetrics"))
                {
                    float x = dm.Get<float>("xdpi"), y = dm.Get<float>("ydpi");
                    if (x > 50f && y > 50f) dpi = (x + y) * 0.5f;
                }
            }
            catch (System.Exception) { dpi = 0f; }
#endif
            if (dpi <= 50f) dpi = Screen.dpi > 50f ? Screen.dpi : 400f;
            // Si se dibuja a menor resolucion que la nativa, hay menos pixeles por pulgada.
            int native = Mathf.Max(Display.main.systemWidth, Display.main.systemHeight);
            if (native > 0) dpi *= Screen.width / (float)native;
            return dpi;
        }

        static Quaternion GyroToUnity(Quaternion q) =>
            Quaternion.Euler(90f, 0f, 0f) * new Quaternion(q.x, q.y, -q.z, -q.w);

        public void Recenter()
        {
            if (useGyro)
            {
                var f = GyroToUnity(Input.gyro.attitude) * Vector3.forward;
                yawOffset = -Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;
                savedYaw = yawOffset;
                centered = true;
            }
            mouseYaw = 0f;
            mousePitch = 0f;
            stickPitch = 0f;
            if (Application.isPlaying && Time.timeSinceLevelLoad > 1f) ShowToast("Vista recentrada", 1.5f);
        }

        void Update()
        {
            float dt = Time.deltaTime;

            // Orientacion de la cabeza
            if (useGyro)
            {
                var att = Input.gyro.attitude;
                if (!centered)
                {
                    // Se recentra solo cuando el celular lleva medio segundo derecho, como dentro del visor
                    // (no mientras esta en la mano o sobre la mesa).
                    float n = att.x * att.x + att.y * att.y + att.z * att.z + att.w * att.w;
                    var u = GyroToUnity(att);
                    Vector3 f = u * Vector3.forward, up = u * Vector3.up;
                    bool upright = n > 0.9f && n < 1.1f && Mathf.Abs(f.y) < 0.5f && up.y > 0.85f;
                    uprightTime = upright ? uprightTime + dt : 0f;
                    if (uprightTime >= 0.5f) Recenter();
                }
                head.localRotation = Quaternion.Euler(0f, yawOffset, 0f) * GyroToUnity(att);
            }
            else if (useTilt)
            {
                // Sin giroscopio: el acelerometro da la inclinacion (mirar arriba/abajo).
                var a = Input.acceleration;
                float target = Mathf.Clamp(-Mathf.Atan2(a.z, -a.y) * Mathf.Rad2Deg, -80f, 80f);
                tiltPitch = Mathf.LerpAngle(tiltPitch, target, 1f - Mathf.Exp(-8f * dt));
                stickPitch = Mathf.Clamp(stickPitch + LabInput.LookVertical * lookSpeed * dt, -60f, 60f);
                head.localRotation = Quaternion.Euler(Mathf.Clamp(tiltPitch + stickPitch, -80f, 80f), 0f, 0f);
            }
            else
            {
                if (Input.GetMouseButton(1))
                {
                    mouseYaw += Input.GetAxis("Mouse X") * mouseSensitivity;
                    mousePitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
                }
                mousePitch = Mathf.Clamp(mousePitch + LabInput.LookVertical * lookSpeed * dt, -80f, 80f);
                head.localRotation = Quaternion.Euler(mousePitch, mouseYaw, 0f);
            }

            // Giro del cuerpo con el joystick derecho
            float turn = LabInput.Turn;
            if (turn != 0f) transform.Rotate(0f, turn * turnSpeed * dt, 0f);

            // Caminar en la direccion a la que se mira
            var move = LabInput.Move;
            var fwd = head.forward; fwd.y = 0f;
            var right = head.right; right.y = 0f;
            if (fwd.sqrMagnitude < 0.001f) fwd = transform.forward;
            var velocity = (fwd.normalized * move.y + right.normalized * move.x) * moveSpeed;
            controller.SimpleMove(velocity);

            if (LabInput.RecenterDown) Recenter();
            if (LabInput.ToggleStereoDown) SetStereo(!Stereo);

            // Ajuste de la separacion de las lentes (L1 / R1 del mando, o [ y ])
            int lens = LabInput.LensAdjust;
            if (lens != 0)
            {
                lensSeparationMm = ClampLensToScreen(lensSeparationMm + lens, PhysicalDpi());
                PlayerPrefs.SetFloat(LensKey, lensSeparationMm);
                PlayerPrefs.Save();
                if (Stereo) LayoutEyes();
                ShowToast($"Separación de lentes: {lensSeparationMm:0} mm", 2f);
            }
            if (Stereo && Screen.width != layoutWidth) LayoutEyes();

            if (toastRoot != null && toastRoot.activeSelf && Time.unscaledTime > toastUntil) toastRoot.SetActive(false);
        }

        /// <summary>Mensaje corto flotando frente a los ojos.</summary>
        public void ShowToast(string message, float seconds)
        {
            if (toast == null)
            {
                toastRoot = new GameObject("Aviso");
                toastRoot.transform.SetParent(head, false);
                toastRoot.transform.localPosition = new Vector3(0f, -0.18f, 1.2f);
                toast = LabFactory.Text(toastRoot.transform, "", Vector3.zero, 0.032f, Color.white, TextAlignmentOptions.Center, 0.85f, 0.15f);
                // Se dibuja encima de todo (sin prueba de profundidad), aunque haya una pared cerca.
                var m = toast.fontMaterial;
                m.SetFloat("unity_GUIZTestMode", (float)UnityEngine.Rendering.CompareFunction.Always);
                m.renderQueue = 4000;
            }
            // El fondo oscuro es parte del mismo texto (<mark>), asi tambien queda encima.
            toast.text = $"<mark=#0D1624E8 padding=\"18,18,10,10\">{message}</mark>";
            toastRoot.SetActive(true);
            toastUntil = Time.unscaledTime + seconds;
        }
    }
}

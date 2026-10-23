using UnityEngine;

namespace LabQuimicaVR
{
    /// <summary>Gira un objeto y opcionalmente lo hace flotar suavemente.</summary>
    public class Spin : MonoBehaviour
    {
        public Vector3 degreesPerSecond = new Vector3(0, 20, 0);
        public float bobHeight = 0f;
        public float bobSpeed = 1f;

        Vector3 basePos;

        void Start() { basePos = transform.localPosition; }

        void Update()
        {
            transform.Rotate(degreesPerSecond * Time.deltaTime, Space.Self);
            if (bobHeight > 0f)
                transform.localPosition = basePos + Vector3.up * Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        }
    }
}

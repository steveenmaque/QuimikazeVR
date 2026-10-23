using UnityEngine;

namespace LabQuimicaVR
{
    /// <summary>Hace que una etiqueta mire siempre hacia la cabeza del jugador.</summary>
    public class FaceCamera : MonoBehaviour
    {
        void LateUpdate()
        {
            var head = GazePointer.Head;
            if (head == null) return;
            var dir = transform.position - head.position;
            if (dir.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }
    }
}

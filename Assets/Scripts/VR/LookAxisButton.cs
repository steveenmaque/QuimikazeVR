using UnityEngine;

namespace LabQuimicaVR
{
    [RequireComponent(typeof(GazeButton))]
    public class LookAxisButton : MonoBehaviour
    {
        GazeButton button;
        void Awake()
        {
            button = GetComponent<GazeButton>();
            button.OnClick += () => VRRig.ToggleRightStickYAxis();
        }
        void Update()
        {
            if (button.Label != null)
                button.Label.text = "Eje Y: " + (LabInput.InvertRightStickY ? "invertido" : "normal");
        }
    }
}

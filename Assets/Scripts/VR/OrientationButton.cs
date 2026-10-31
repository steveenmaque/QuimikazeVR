using UnityEngine;

namespace LabQuimicaVR
{
    /// <summary>
    /// Boton para elegir como gira la pantalla: automatica (sigue al celular, para ponerlo en el visor
    /// de un lado o del otro) o fija a un lado. Se guarda para la proxima vez.
    /// </summary>
    [RequireComponent(typeof(GazeButton))]
    public class OrientationButton : MonoBehaviour
    {
        GazeButton button;

        void Awake()
        {
            button = GetComponent<GazeButton>();
            button.OnClick += () => { VRRig.CycleOrientation(); Refresh(); };
        }

        void Start() => Refresh();

        void Refresh()
        {
            if (button.Label != null) button.Label.text = "Pantalla: " + VRRig.OrientationNames[VRRig.OrientationMode];
        }
    }
}

using System;
using UnityEngine;

namespace LabQuimicaVR
{
    /// <summary>Cualquier objeto que se puede mirar y pulsar.</summary>
    public class GazeTarget : MonoBehaviour
    {
        public event Action OnHoverEnter;
        public event Action OnHoverExit;
        public event Action OnClick;

        public virtual bool Interactable => true;
        public bool IsHovered { get; private set; }

        public void HoverEnter() { IsHovered = true; OnHoverEnter?.Invoke(); HoverChanged(true); }
        public void HoverExit() { IsHovered = false; OnHoverExit?.Invoke(); HoverChanged(false); }
        public void Click() { OnClick?.Invoke(); Clicked(); }

        protected virtual void HoverChanged(bool hovered) { }
        protected virtual void Clicked() { }
    }
}

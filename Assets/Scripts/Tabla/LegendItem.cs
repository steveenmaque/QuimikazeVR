using UnityEngine;

namespace LabQuimicaVR
{
    /// <summary>Elemento de la leyenda: al mirarlo resalta esa familia en la tabla.</summary>
    public class LegendItem : GazeTarget
    {
        public ElementCategory category;
        public GameObject outline;

        static LegendItem pinned;

        protected override void HoverChanged(bool hovered)
        {
            if (outline != null) outline.SetActive(hovered || pinned == this);
            var view = PeriodicTableView.Instance;
            if (view == null) return;
            if (hovered) view.Highlight(category);
            else view.Highlight(pinned != null ? pinned.category : (ElementCategory?)null);
        }

        protected override void Clicked()
        {
            var previous = pinned;
            pinned = pinned == this ? null : this;
            if (previous != null && previous != this && previous.outline != null) previous.outline.SetActive(false);
            LabAudio.Play(LabSound.Chime, 0.5f);
        }

        void OnDestroy()
        {
            if (pinned == this) pinned = null;
        }
    }
}

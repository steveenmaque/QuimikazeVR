using TMPro;
using UnityEngine;

namespace LabQuimicaVR
{
    /// <summary>Boton 3D que se ilumina al mirarlo.</summary>
    public class GazeButton : GazeTarget
    {
        [SerializeField] Renderer body;
        [SerializeField] GameObject outline;
        [SerializeField] TextMeshPro label;
        [SerializeField] Material idleMaterial;

        public bool interactable = true;
        public override bool Interactable => interactable;

        Vector3 baseScale = Vector3.one;
        float hover;
        Color idleLabelColor = Color.white;
        bool hasIdleLabelColor;
        bool isSelected;
        bool lastInteractable = true;

        public TextMeshPro Label => label;

        public void Setup(Renderer body, GameObject outline, TextMeshPro label, Material idle)
        {
            this.body = body;
            this.outline = outline;
            this.label = label;
            idleMaterial = idle;
            if (this.label != null)
            {
                idleLabelColor = this.label.color;
                hasIdleLabelColor = true;
            }
            RefreshVisuals();
        }

        void Awake()
        {
            baseScale = transform.localScale;
            if (label != null && !hasIdleLabelColor)
            {
                idleLabelColor = label.color;
                hasIdleLabelColor = true;
            }
            RefreshVisuals();
        }

        public void SetSelected(bool value)
        {
            isSelected = value;
            if (body != null) body.sharedMaterial = value ? LabFactory.Mat("ButtonActive") : idleMaterial;
            RefreshVisuals();
        }

        protected override void HoverChanged(bool hovered)
        {
            if (outline != null) outline.SetActive(hovered && interactable);
        }

        void Update()
        {
            if (interactable != lastInteractable) RefreshVisuals();
            hover = interactable ? Mathf.MoveTowards(hover, IsHovered ? 1f : 0f, Time.deltaTime * 8f) : 0f;
            transform.localScale = baseScale * (1f + hover * 0.08f);
        }

        void RefreshVisuals()
        {
            lastInteractable = interactable;
            if (!interactable && outline != null) outline.SetActive(false);
            if (label == null) return;
            if (!hasIdleLabelColor)
            {
                idleLabelColor = label.color;
                hasIdleLabelColor = true;
            }
            label.color = interactable || isSelected
                ? idleLabelColor
                : new Color(idleLabelColor.r * 0.48f, idleLabelColor.g * 0.48f, idleLabelColor.b * 0.48f, idleLabelColor.a * 0.55f);
        }
    }
}

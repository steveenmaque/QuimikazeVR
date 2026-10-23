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

        public TextMeshPro Label => label;

        public void Setup(Renderer body, GameObject outline, TextMeshPro label, Material idle)
        {
            this.body = body;
            this.outline = outline;
            this.label = label;
            idleMaterial = idle;
        }

        void Awake() { baseScale = transform.localScale; }

        public void SetSelected(bool value)
        {
            if (body != null) body.sharedMaterial = value ? LabFactory.Mat("ButtonActive") : idleMaterial;
        }

        protected override void HoverChanged(bool hovered)
        {
            if (outline != null) outline.SetActive(hovered);
        }

        void Update()
        {
            hover = Mathf.MoveTowards(hover, IsHovered ? 1f : 0f, Time.deltaTime * 8f);
            transform.localScale = baseScale * (1f + hover * 0.08f);
        }
    }
}

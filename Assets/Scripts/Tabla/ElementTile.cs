using TMPro;
using UnityEngine;

namespace LabQuimicaVR
{
    /// <summary>Casilla de un elemento en la tabla periodica.</summary>
    public class ElementTile : GazeTarget
    {
        public int atomicNumber;
        public Renderer body;
        public GameObject outline;
        public Material normalMaterial;
        public TextMeshPro[] labels;

        float hover;
        bool dimmed;
        Vector3 basePos;
        bool started;

        public ElementCategory Category => ElementDatabase.Get(atomicNumber).Category;

        void Start()
        {
            basePos = transform.localPosition;
            started = true;
        }

        protected override void HoverChanged(bool hovered)
        {
            if (outline != null) outline.SetActive(hovered);
            if (hovered && PeriodicTableView.Instance != null) PeriodicTableView.Instance.ShowPreview(atomicNumber);
            if (!hovered && PeriodicTableView.Instance != null) PeriodicTableView.Instance.ClearPreview(atomicNumber);
        }

        protected override void Clicked()
        {
            LabState.OpenElement(atomicNumber);
        }

        public void SetDimmed(bool value)
        {
            if (dimmed == value) return;
            dimmed = value;
            body.sharedMaterial = value ? LabFactory.Mat("TileDim") : normalMaterial;
            // Solo se cambia la transparencia de los vertices: TextMeshPro no vuelve a armar el texto.
            byte a = value ? (byte)90 : (byte)255;
            foreach (var l in labels)
            {
                if (l == null) continue;
                var info = l.textInfo;
                if (info == null || info.characterCount == 0) { l.ForceMeshUpdate(); info = l.textInfo; }
                for (int m = 0; m < info.materialCount; m++)
                {
                    var colors = info.meshInfo[m].colors32;
                    if (colors == null) continue;
                    for (int i = 0; i < colors.Length; i++) colors[i].a = a;
                }
                l.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
            }
        }

        void Update()
        {
            if (!started) return;
            float target = IsHovered ? 1f : 0f;
            if (Mathf.Approximately(hover, target)) return;
            hover = Mathf.MoveTowards(hover, target, Time.deltaTime * 7f);
            float e = hover * hover * (3f - 2f * hover);
            transform.localPosition = basePos + new Vector3(0, 0, -0.18f * e);
            transform.localScale = Vector3.one * (1f + 0.22f * e);
        }
    }
}

using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace LabQuimicaVR
{
    /// <summary>
    /// Pantalla principal: la tabla periodica completa. Muestra una vista previa del elemento
    /// que estas mirando y permite resaltar familias desde la leyenda.
    /// </summary>
    public class PeriodicTableView : MonoBehaviour
    {
        public static PeriodicTableView Instance { get; private set; }

        public const float Pitch = 0.6f;
        public const float TileSize = 0.55f;
        public const float TopY = 6.25f;

        public List<ElementTile> tiles = new List<ElementTile>();

        [Header("Vista previa")]
        public GameObject previewContent;
        public GameObject previewHint;
        public Renderer previewTile;
        public TextMeshPro previewNumber;
        public TextMeshPro previewSymbol;
        public TextMeshPro previewName;
        public TextMeshPro previewTitle;
        public TextMeshPro previewInfo;

        int shownZ;

        public static Vector3 Cell(int row, int col)
        {
            float rowOffset = row >= 9 ? row - 1.5f : row - 1f;
            return new Vector3((col - 9.5f) * Pitch, TopY - rowOffset * Pitch, 0f);
        }

        float backPressedAt = -10f;

        void Awake() { Instance = this; }

        void Update()
        {
            // Boton "atras" del celular en la pantalla principal: pulsarlo dos veces para salir.
            if (!Input.GetKeyDown(KeyCode.Escape)) return;
            if (Time.unscaledTime - backPressedAt < 2f)
            {
                Application.Quit();
                return;
            }
            backPressedAt = Time.unscaledTime;
            if (VRRig.Instance != null) VRRig.Instance.ShowToast("Pulsa atrás otra vez para salir", 2f);
        }
        void OnDestroy() { if (Instance == this) Instance = null; }

        void Start()
        {
            ShowHint();
            // Si venimos de un elemento, lo mostramos en la vista previa.
            if (LabState.Visited) ShowPreview(LabState.SelectedZ);
        }

        public void ShowPreview(int z)
        {
            var e = ElementDatabase.Get(z);
            shownZ = z;
            if (previewHint != null) previewHint.SetActive(false);
            if (previewContent != null) previewContent.SetActive(true);
            if (previewTile != null) previewTile.sharedMaterial = LabFactory.Mat("Cat_" + CategoryInfo.Code(e.Category));
            if (previewNumber != null) previewNumber.text = e.Z.ToString();
            if (previewSymbol != null) previewSymbol.text = e.Symbol;
            if (previewName != null) previewName.text = e.Name;
            if (previewTitle != null) previewTitle.text = $"<b>{e.Name}</b>  <color=#CCD4FF><size=60%>{CategoryInfo.Name(e.Category)}</size></color>";
            if (previewInfo != null)
            {
                previewInfo.text =
                    $"Número atómico: <b>{e.Z}</b>     Masa atómica: <b>{e.MassText} u</b>\n" +
                    $"Grupo: <b>{PeriodicLayout.GroupLabel(e)}</b>  ·  Periodo (fila): <b>{e.Period}</b>  ·  Bloque <b>{e.Block}</b>\n" +
                    $"Familia: <b>{PeriodicLayout.GroupFamilyName(e)}</b>\n" +
                    $"Configuración: <b>{ElectronConfiguration.Kernel(e)}</b>\n" +
                    $"<color=#FFC34D>Pulsa el botón para entrar a su laboratorio</color>";
            }
        }

        public void ClearPreview(int z)
        {
            // Mantiene el ultimo elemento visible; asi se puede leer al apartar la vista.
        }

        void ShowHint()
        {
            if (previewHint != null) previewHint.SetActive(true);
            if (previewContent != null) previewContent.SetActive(false);
        }

        public void Highlight(ElementCategory? category)
        {
            foreach (var t in tiles)
            {
                if (t == null) continue;
                t.SetDimmed(category.HasValue && t.Category != category.Value);
            }
        }
    }
}

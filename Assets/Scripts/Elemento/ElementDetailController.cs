using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace LabQuimicaVR
{
    /// <summary>
    /// "Nivel" de un elemento: el modelo 3D al frente, la historia y linea de tiempo en la pared
    /// izquierda, las propiedades en la derecha, el laboratorio de reacciones y los controles
    /// a los lados del modelo, y el boton para volver en la pared de atras.
    /// </summary>
    public class ElementDetailController : MonoBehaviour
    {
        [Header("Anclas (las coloca el constructor de escenas)")]
        public Transform titleAnchor;
        public Transform modelAnchor;
        public Transform historyAnchor;
        public Transform propsAnchor;
        public Transform reactionAnchor;
        public Transform controlsAnchor;
        public Transform sampleAnchor;
        public Transform backAnchor;
        public Transform discoverersAnchor;
        public Transform natureAnchor;
        public Transform landscapeAnchor;

        [Tooltip("Elemento que se muestra en el editor (vista previa)")]
        public int previewZ = 6;

        AtomModel atom;
        TextMeshPro modeTitle, modeInfo;
        readonly List<(AtomViewMode mode, GazeButton button)> modeButtons = new List<(AtomViewMode, GazeButton)>();
        ElementInfo element;

        static readonly Color DarkText = new Color(0.11f, 0.13f, 0.17f);

        void Start()
        {
            if (!Application.isPlaying) return;
            Populate(LabState.SelectedZ);
            ElementIntro.Play(element, ElementExtras.Landscape(ElementExtras.Sample(element).Landscape));
        }

        void Update()
        {
            if (!Application.isPlaying || element == null || ElementIntro.Active) return;
            if (LabInput.BackDown) LabState.BackToTable();
            if (LabInput.ModeDown && atom != null) SetMode(atom.NextMode());
        }

        public void Populate(int z)
        {
            element = ElementDatabase.Get(z);
            modeButtons.Clear();
            BuildTitle();
            BuildModel();
            BuildHistory();
            BuildProperties();
            BuildControls();
            BuildBack();
            BuildDiscoverers();
            BuildNature();
            BuildLandscape();
            if (sampleAnchor != null) { LabFactory.ClearChildren(sampleAnchor); ElementSample.Build(sampleAnchor, element); }
            if (reactionAnchor != null)
            {
                var station = reactionAnchor.GetComponent<ReactionStation>();
                if (station == null) station = reactionAnchor.gameObject.AddComponent<ReactionStation>();
                station.Build(element);
            }
            SetMode(AtomViewMode.Bohr);
        }

        // ---------------- Titulo (pantalla del frente) ----------------
        void BuildTitle()
        {
            var e = element;
            var root = titleAnchor;
            LabFactory.ClearChildren(root);
            var catColor = CategoryInfo.Color(e.Category);

            var tile = LabFactory.Box("Casilla", root, new Vector3(-2.2f, 0, -0.02f), new Vector3(1.15f, 1.15f, 0.04f), LabFactory.Mat("Cat_" + CategoryInfo.Code(e.Category)));
            tile.name = "Casilla";
            LabFactory.Text(root, e.Z.ToString(), new Vector3(-2.2f, 0.41f, -0.05f), 0.14f, DarkText, TextAlignmentOptions.TopLeft, 1.0f, 0.2f, true);
            LabFactory.Text(root, e.Symbol, new Vector3(-2.2f, 0.03f, -0.05f), 0.46f, DarkText, TextAlignmentOptions.Center, 1.1f, 0.65f, true, TextRole.Symbol);
            LabFactory.Text(root, e.MassText, new Vector3(-2.2f, -0.42f, -0.05f), 0.09f, DarkText, TextAlignmentOptions.Center, 1.1f, 0.16f);

            var name = LabFactory.Text(root, e.Name.ToUpper(), new Vector3(0.55f, 0.32f, -0.02f), 0.44f, Color.white, TextAlignmentOptions.Left, 4.1f, 0.55f, true);
            name.textWrappingMode = TextWrappingModes.NoWrap;
            name.enableAutoSizing = true;
            name.fontSizeMax = 4.4f;
            name.fontSizeMin = 2.4f;
            string chips = $"<color=#{ColorUtility.ToHtmlStringRGB(catColor)}>■</color> {CategoryInfo.Name(e.Category)}  ·  Grupo {PeriodicLayout.GroupLabel(e)}  ·  Periodo {e.Period}";
            if (e.IsRadioactive) chips += "  ·  <color=#FFB04D>Radiactivo</color>";
            var chipText = LabFactory.Text(root, chips, new Vector3(0.55f, -0.08f, -0.02f), 0.14f, Palette.TextDim, TextAlignmentOptions.Left, 4.1f, 0.24f);
            AutoFit(chipText, 0.14f, 0.08f);
            chipText.textWrappingMode = TextWrappingModes.NoWrap;

            modeTitle = null;
            modeInfo = LabFactory.Text(root, "", new Vector3(0.55f, -0.4f, -0.02f), 0.12f, Color.white, TextAlignmentOptions.Left, 4.1f, 0.34f);
            AutoFit(modeInfo, 0.12f, 0.07f);
        }

        // ---------------- Modelo 3D ----------------
        void BuildModel()
        {
            LabFactory.ClearChildren(modelAnchor);
            atom = LabFactory.Node("Atomo", modelAnchor).AddComponent<AtomModel>();
            atom.Build(element);
            // Todos los atomos se muestran de un tamano parecido, sin importar cuantos niveles tengan.
            float outer = AtomModel.ShellRadius(Mathf.Max(1, element.Shells.Length));
            atom.transform.localScale = Vector3.one * Mathf.Clamp(1.2f / outer, 0.8f, 2.4f);
        }

        public void SetMode(AtomViewMode mode)
        {
            if (atom == null) return;
            atom.SetMode(mode);
            var e = element;
            switch (mode)
            {
                case AtomViewMode.Bohr:
                    modeInfo.text = $"<color=#FFC34D><b>Modelo de Bohr:</b></color>  <color=#FF5A55>●</color> {e.Z} protones   <color=#B0BEC5>●</color> {e.Neutrons} neutrones   <color=#29B6F6>●</color> {e.Z} electrones en {e.Shells.Length} niveles";
                    break;
                case AtomViewMode.Cloud:
                    modeInfo.text = "<color=#FFC34D><b>Nube electrónica:</b></color>  <color=#2BB5E8>●</color> s   <color=#2ECC52>●</color> p   <color=#F2A21F>●</color> d   <color=#F0509F>●</color> f  · donde es más probable hallar cada electrón";
                    break;
                case AtomViewMode.Nucleus:
                    modeInfo.text = $"<color=#FFC34D><b>Núcleo:</b></color>  {e.Z} protones (+) y {e.Neutrons} neutrones. ¡El núcleo real es unas 100 000 veces más pequeño que el átomo!";
                    break;
            }
            foreach (var (m, b) in modeButtons) b.SetSelected(m == mode);
            if (Application.isPlaying) LabAudio.Play(LabSound.Chime, 0.4f);
        }

        // ---------------- Historia (pared izquierda) ----------------
        void BuildHistory()
        {
            var e = element;
            LabFactory.ClearChildren(historyAnchor);
            var size = new Vector2(7.2f, 3.7f);
            var s = LabFactory.Screen("PantallaHistoria", historyAnchor, Vector3.zero, Quaternion.identity, size, $"HISTORIA {e.Del.ToUpper()} {e.Name.ToUpper()}");
            float left = -size.x / 2 + 0.25f, right = size.x / 2 - 0.25f;

            // Linea de tiempo
            LabFactory.Text(s, "LÍNEA DE TIEMPO", new Vector3(left + 1.0f, 1.14f, -0.01f), 0.08f, Palette.Accent2, TextAlignmentOptions.Left, 2f, 0.15f, true);
            float lineY = 0.8f;
            LabFactory.Box("Linea", s, new Vector3(0, lineY, -0.01f), new Vector3(right - left, 0.02f, 0.01f), LabFactory.Tinted("GlowBase", Palette.Accent, 1.3f));
            var events = e.Timeline.Count > 0 ? e.Timeline : new List<TimelineEvent> { new TimelineEvent { Year = e.Year, Text = "Descubrimiento" } };
            int n = Mathf.Min(events.Count, 5);
            float span = (right - left) / n;
            for (int i = 0; i < n; i++)
            {
                float x = left + span * (i + 0.5f);
                LabFactory.Shape("Punto", PrimitiveType.Sphere, s, new Vector3(x, lineY, -0.02f), Vector3.one * 0.09f, LabFactory.Tinted("GlowBase", Palette.Accent2, 1.6f));
                LabFactory.Text(s, events[i].Year, new Vector3(x, lineY + 0.16f, -0.01f), 0.12f, Palette.Accent2, TextAlignmentOptions.Center, span - 0.05f, 0.2f, true);
                var t = LabFactory.Text(s, events[i].Text, new Vector3(x, lineY - 0.34f, -0.01f), 0.1f, Palette.Text, TextAlignmentOptions.Top, span - 0.1f, 0.52f);
                AutoFit(t, 0.1f, 0.055f);
            }

            // Descubrimiento
            var card = new Vector3(0, -0.15f, 0);
            LabFactory.Card(s, card, new Vector2(right - left, 0.66f), Palette.CardBg);
            var disc = LabFactory.Text(s, $"<color=#FFC34D><b>Descubridor:</b></color> {e.Discoverer}  <color=#9FB4CC>({e.Year})</color>",
                card + new Vector3(0, 0.2f, -0.02f), 0.12f, Color.white, TextAlignmentOptions.Left, right - left - 0.2f, 0.2f);
            AutoFit(disc, 0.12f, 0.06f);
            var hist = LabFactory.Text(s, e.History, card + new Vector3(0, -0.09f, -0.02f), 0.1f, Palette.Text, TextAlignmentOptions.TopLeft, right - left - 0.2f, 0.4f);
            AutoFit(hist, 0.1f, 0.055f);

            // Tres tarjetas
            string[] titles = { "ORIGEN DEL NOMBRE", "APORTES Y VALOR", "¿SABÍAS QUE...?" };
            string[] bodies = { e.NameOrigin, e.Value, e.Curiosity };
            Color[] colors = { new Color(0.13f, 0.24f, 0.36f), new Color(0.12f, 0.3f, 0.26f), new Color(0.32f, 0.2f, 0.36f) };
            float cw = (right - left - 0.2f) / 3f;
            for (int i = 0; i < 3; i++)
            {
                float x = left + cw / 2 + i * (cw + 0.1f);
                var c = new Vector3(x, -1.14f, 0);
                LabFactory.Card(s, c, new Vector2(cw, 1.1f), colors[i]);
                LabFactory.Text(s, titles[i], c + new Vector3(0, 0.42f, -0.02f), 0.09f, Palette.Accent2, TextAlignmentOptions.Center, cw - 0.1f, 0.16f, true);
                var body = LabFactory.Text(s, bodies[i], c + new Vector3(0, -0.09f, -0.02f), 0.12f, Color.white, TextAlignmentOptions.Top, cw - 0.14f, 0.82f);
                AutoFit(body, 0.12f, 0.055f);
            }
        }

        // ---------------- Propiedades (pared derecha) ----------------
        void BuildProperties()
        {
            var e = element;
            LabFactory.ClearChildren(propsAnchor);
            var size = new Vector2(7.2f, 3.7f);
            var s = LabFactory.Screen("PantallaPropiedades", propsAnchor, Vector3.zero, Quaternion.identity, size, $"PROPIEDADES {e.Del.ToUpper()} {e.Name.ToUpper()}");

            float left = -size.x / 2 + 0.2f;
            float width = size.x - 0.4f;
            float cellW = width / 4f;
            float rowH = 0.5f;
            float top = 1.18f;

            void Prop(int row, int col, int span, string label, string value, Color? tint = null)
            {
                float w = cellW * span - 0.08f;
                var center = new Vector3(left + cellW * col + cellW * span / 2f, top - row * rowH, 0);
                LabFactory.Card(s, center, new Vector2(w, rowH - 0.07f), tint ?? Palette.CardBg);
                var l = LabFactory.Text(s, label, center + new Vector3(0, 0.13f, -0.02f), 0.085f, Palette.TextDim, TextAlignmentOptions.Center, w - 0.06f, 0.13f);
                l.textWrappingMode = TextWrappingModes.NoWrap;
                AutoFit(l, 0.085f, 0.05f);
                var v = LabFactory.Text(s, value, center + new Vector3(0, -0.06f, -0.02f), 0.15f, Color.white, TextAlignmentOptions.Center, w - 0.08f, 0.25f, true);
                v.textWrappingMode = TextWrappingModes.NoWrap;
                AutoFit(v, 0.15f, 0.06f);
            }

            var catTint = Color.Lerp(Palette.CardBg, CategoryInfo.Color(e.Category), 0.35f);
            Prop(0, 0, 1, "Número atómico (Z)", e.Z.ToString());
            Prop(0, 1, 1, "Masa atómica", e.MassText + " u");
            Prop(0, 2, 1, "Grupo (columna)", PeriodicLayout.GroupLabel(e));
            Prop(0, 3, 1, "Periodo (fila)", e.Period.ToString());

            Prop(1, 0, 2, "Nombre del grupo", PeriodicLayout.GroupFamilyName(e), catTint);
            Prop(1, 2, 1, "Categoría", CategoryInfo.Name(e.Category) + (e.Z >= 109 ? " (predicha)" : ""), catTint);
            Prop(1, 3, 1, "Bloque", e.Block.ToString());

            Prop(2, 0, 1, "Estado a 25 °C", e.StateName);
            Prop(2, 1, 1, "Electronegatividad", e.Electronegativity);
            Prop(2, 2, 1, "Punto de fusión", Temp(e.Melting));
            Prop(2, 3, 1, "Punto de ebullición", Temp(e.Boiling));

            Prop(3, 0, 1, "Densidad", e.Density);
            Prop(3, 1, 2, "Protones · Neutrones · Electrones", $"{e.Z} p<sup>+</sup>   ·   {e.Neutrons} n<sup>0</sup>   ·   {e.Z} e<sup>-</sup>");
            Prop(3, 3, 1, "Radiactivo", e.IsRadioactive ? "Sí" : "No", e.IsRadioactive ? new Color(0.45f, 0.22f, 0.12f) : (Color?)null);

            Prop(4, 0, 2, "Configuración electrónica (Moeller)", ElectronConfiguration.Full(e));
            Prop(4, 2, 1, "Configuración abreviada", ElectronConfiguration.Kernel(e));
            Prop(4, 3, 1, "Electrones por nivel", ElectronConfiguration.ShellsText(e));

            Prop(5, 0, 2, "Valencias (estados de oxidación)", ValencesText(e));
            Prop(5, 2, 2, "Equivalente químico (masa ÷ valencia)", EquivalentsText(e));

            var desc = LabFactory.Text(s, $"<color=#FFC34D>{CategoryInfo.Name(e.Category)}:</color> {CategoryInfo.Description(e.Category)}",
                new Vector3(0, top - 6 * rowH + 0.1f, -0.02f), 0.075f, Palette.TextDim, TextAlignmentOptions.Center, width, 0.22f);
            AutoFit(desc, 0.075f, 0.05f);
        }

        static string Temp(string v) => v == "—" || v == "?" ? "Desconocido" : v + " °C";

        static string ValencesText(ElementInfo e)
        {
            if (e.ValencesUnknown) return "Desconocidas";
            if (e.Valences.All(v => v == 0)) return "0 (casi no forma compuestos)";
            return string.Join("   ", e.Valences.Select(v => v > 0 ? "+" + v : v.ToString()));
        }

        static string EquivalentsText(ElementInfo e)
        {
            if (e.ValencesUnknown || e.Mass <= 0) return "No se conoce";
            var vals = e.Valences.Select(Mathf.Abs).Where(v => v > 0).Distinct().OrderBy(v => v).Take(4).ToList();
            if (vals.Count == 0) return "No aplica (no forma compuestos)";
            return string.Join("   ", vals.Select(v => $"val {v}: {(e.Mass / v).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)} g"));
        }

        // ---------------- Controles (adelante a la derecha) ----------------
        void BuildControls()
        {
            LabFactory.ClearChildren(controlsAnchor);
            var size = new Vector2(2.6f, 2.6f);
            var s = LabFactory.Screen("PantallaControles", controlsAnchor, Vector3.zero, Quaternion.identity, size, "MODELO DEL ÁTOMO");

            var modes = new[] { (AtomViewMode.Bohr, "Modelo de Bohr"), (AtomViewMode.Cloud, "Nube electrónica"), (AtomViewMode.Nucleus, "Núcleo") };
            for (int i = 0; i < modes.Length; i++)
            {
                var (mode, label) = modes[i];
                var b = LabFactory.Button(s, label, new Vector3(0, 0.62f - i * 0.38f, -0.03f), new Vector2(2.1f, 0.3f), 0.1f);
                b.OnClick += () => SetMode(mode);
                modeButtons.Add((mode, b));
            }

            int z = element.Z;
            var prev = LabFactory.Button(s, "« Anterior", new Vector3(-0.56f, -0.55f, -0.03f), new Vector2(1.0f, 0.3f), 0.09f, new Color(0.25f, 0.3f, 0.4f));
            prev.interactable = z > 1;
            prev.OnClick += () => LabState.OpenElement(z - 1);
            var next = LabFactory.Button(s, "Siguiente »", new Vector3(0.56f, -0.55f, -0.03f), new Vector2(1.0f, 0.3f), 0.09f, new Color(0.25f, 0.3f, 0.4f));
            next.interactable = z < 118;
            next.OnClick += () => LabState.OpenElement(z + 1);

            LabFactory.Text(s, "Mira un botón y toca el botón del visor (o A)\nX: cambiar modelo  ·  B: volver a la tabla", new Vector3(0, -0.98f, -0.01f), 0.075f, Palette.TextDim,
                TextAlignmentOptions.Center, size.x - 0.2f, 0.3f);
        }

        // ---------------- Pared de atras ----------------
        void BuildBack()
        {
            LabFactory.ClearChildren(backAnchor);
            var s = LabFactory.Screen("PantallaVolver", backAnchor, Vector3.zero, Quaternion.identity, new Vector2(4.2f, 1.3f), "SALIDA DEL LABORATORIO");
            LabFactory.Text(s, $"Estás en el laboratorio {element.Del} <b>{element.Name.ToLower()}</b>", new Vector3(0, 0.2f, -0.01f), 0.09f, Palette.Text, TextAlignmentOptions.Center, 4f, 0.16f);
            var back = LabFactory.Button(s, "Volver a la tabla periódica", new Vector3(0, -0.17f, -0.03f), new Vector2(3.0f, 0.36f), 0.12f, new Color(0.0f, 0.45f, 0.6f));
            back.OnClick += LabState.BackToTable;
            LabFactory.Text(s, "También puedes pulsar el botón B del mando", new Vector3(0, -0.5f, -0.01f), 0.055f, Palette.TextDim, TextAlignmentOptions.Center, 4f, 0.12f);
        }

        // ---------------- Descubridores con sus retratos (pared izquierda, atras) ----------------
        void BuildDiscoverers()
        {
            if (discoverersAnchor == null) return;
            var e = element;
            LabFactory.ClearChildren(discoverersAnchor);
            var size = new Vector2(5.6f, 3.7f);
            var people = ElementExtras.Discoverers(e.Z);
            bool ancient = people.Count > 0 && people.All(p => p.Kind == 'H');
            string title = ancient ? "CONOCIDO DESDE LA ANTIGÜEDAD" : people.Any(p => p.Kind == 'I') && !people.Any(p => p.IsPerson) ? "DÓNDE LO CREARON" : "QUIÉN LO DESCUBRIÓ";
            var s = LabFactory.Screen("PantallaDescubridores", discoverersAnchor, Vector3.zero, Quaternion.identity, size, title, Palette.Accent2);

            string header = e.Discoverer.StartsWith("Conocid") ? e.Discoverer : $"{e.Discoverer}  <color=#9FB4CC>· {e.Year}</color>";
            var line = LabFactory.Text(s, $"<color=#FFC34D><b>{header}</b></color>", new Vector3(0, 1.1f, -0.01f), 0.11f,
                Color.white, TextAlignmentOptions.Center, size.x - 0.3f, 0.2f);
            AutoFit(line, 0.12f, 0.07f);
            line.textWrappingMode = TextWrappingModes.NoWrap;

            int n = Mathf.Clamp(people.Count, 1, 4);
            float slot = (size.x - 0.3f) / n;
            float imgH = n <= 2 ? 1.75f : 1.5f;
            for (int i = 0; i < n; i++)
            {
                var p = i < people.Count ? people[i] : null;
                float x = -size.x / 2 + 0.15f + slot * (i + 0.5f);
                var cardC = new Vector3(x, -0.36f, 0);
                LabFactory.Card(s, cardC, new Vector2(slot - 0.12f, 2.6f), new Color(0.1f, 0.16f, 0.26f));
                var tex = p?.LoadImage();
                float imgW = slot - 0.3f;
                var imgCenter = cardC + new Vector3(0, 0.45f, -0.015f);
                if (tex != null)
                {
                    float aspect = tex.width / (float)Mathf.Max(1, tex.height);
                    float h = imgH, w = h * aspect;
                    if (w > imgW) { w = imgW; h = w / aspect; }
                    LabFactory.Box("Marco", s, imgCenter + new Vector3(0, 0, 0.009f), new Vector3(w + 0.06f, h + 0.06f, 0.006f), LabFactory.Mat("ScreenFrame"));
                    LabFactory.Shape("Retrato", PrimitiveType.Quad, s, imgCenter, new Vector3(w, h, 1f), ElementSample.PhotoMaterial(tex));
                }
                else
                {
                    // Silueta cuando no existe un retrato conocido
                    var sil = LabFactory.Tinted("Card", new Color(0.25f, 0.32f, 0.44f));
                    LabFactory.Shape("Cabeza", PrimitiveType.Sphere, s, imgCenter + new Vector3(0, 0.3f, 0), new Vector3(0.42f, 0.5f, 0.02f), sil);
                    LabFactory.Shape("Hombros", PrimitiveType.Sphere, s, imgCenter + new Vector3(0, -0.4f, 0), new Vector3(0.9f, 0.6f, 0.02f), sil);
                    LabFactory.Text(s, p != null && !p.IsPerson ? "Sin imagen" : "Sin retrato conocido", imgCenter + new Vector3(0, -0.85f, -0.01f), 0.06f, Palette.TextDim, TextAlignmentOptions.Center, imgW, 0.12f);
                }
                string name = p == null ? e.Discoverer : p.Name;
                string info = p == null ? "" : p.IsPerson ? $"{p.Years}\n{p.Description}" : "";
                var nameText = LabFactory.Text(s, $"<b>{name}</b>", cardC + new Vector3(0, -0.72f, -0.015f), 0.1f, Color.white, TextAlignmentOptions.Center, slot - 0.2f, p != null && !p.IsPerson ? 0.5f : 0.3f);
                AutoFit(nameText, p != null && !p.IsPerson ? 0.075f : 0.1f, 0.05f);
                if (info.Length > 0)
                {
                    var infoText = LabFactory.Text(s, info, cardC + new Vector3(0, -1.08f, -0.015f), 0.075f, Palette.TextDim, TextAlignmentOptions.Center, slot - 0.2f, 0.38f);
                    AutoFit(infoText, 0.075f, 0.045f);
                }
                if (p != null && tex != null)
                {
                    var credit = LabFactory.Text(s, ElementExtras.Credit(p.Author, p.License), cardC + new Vector3(0, -1.47f, -0.015f), 0.035f, new Color(0.45f, 0.53f, 0.65f),
                        TextAlignmentOptions.Center, slot - 0.2f, 0.12f);
                    AutoFit(credit, 0.035f, 0.025f);
                }
            }
        }

        // ---------------- Donde se encuentra en la naturaleza (pared derecha, atras) ----------------
        void BuildNature()
        {
            if (natureAnchor == null) return;
            var e = element;
            var sample = ElementExtras.Sample(e);
            var land = ElementExtras.Landscape(sample.Landscape);
            LabFactory.ClearChildren(natureAnchor);
            var size = new Vector2(5.6f, 3.7f);
            var s = LabFactory.Screen("PantallaNaturaleza", natureAnchor, Vector3.zero, Quaternion.identity, size, "¿DÓNDE SE ENCUENTRA?", Palette.Good);

            // Vista del paisaje (parte central de la foto 360)
            var tex = land?.Load();
            var imgC = new Vector3(-1.25f, 0.35f, -0.015f);
            if (tex != null)
            {
                var m = ElementSample.PhotoMaterial(tex);
                m.mainTextureScale = new Vector2(0.32f, 0.5f);
                m.mainTextureOffset = new Vector2(0.34f, 0.27f);
                LabFactory.Box("Marco", s, imgC + new Vector3(0, 0, 0.009f), new Vector3(2.66f, 1.66f, 0.006f), LabFactory.Mat("ScreenFrame"));
                LabFactory.Shape("Paisaje", PrimitiveType.Quad, s, imgC, new Vector3(2.6f, 1.6f, 1f), m);
                LabFactory.Text(s, $"<b>{land.Title}</b>\n<size=60%><color=#7F93AD>{ElementExtras.Credit(land.Author, land.License)}</color></size>",
                    imgC + new Vector3(0, -1.02f, 0), 0.1f, Palette.Accent2, TextAlignmentOptions.Center, 2.6f, 0.3f);
            }
            var nature = LabFactory.Text(s, sample.Nature, new Vector3(1.4f, 0.55f, -0.015f), 0.13f, Color.white, TextAlignmentOptions.TopLeft, 2.5f, 1.6f);
            AutoFit(nature, 0.13f, 0.07f);
            nature.rectTransform.sizeDelta = new Vector2(2.5f, 0.95f);
            nature.rectTransform.localPosition = new Vector3(1.4f, 0.62f, -0.015f);
            var looks = LabFactory.Text(s, $"<color=#7FE0A0><b>¿Cómo es?</b></color> {sample.Looks}", new Vector3(1.4f, -0.45f, -0.015f), 0.11f, Palette.Text,
                TextAlignmentOptions.TopLeft, 2.5f, 0.85f);
            AutoFit(looks, 0.11f, 0.06f);

            // Datos rapidos
            string origin = e.IsSynthetic && e.Z != 43 && e.Z != 61 ? "Sintético: se crea en laboratorios" :
                            e.Z == 43 || e.Z == 61 ? "Casi no existe natural: se fabrica" : "Natural: existe en la Tierra";
            string[] chips =
            {
                origin,
                $"Estado a 25 °C: {e.StateName.ToLower()}",
                e.IsRadioactive ? "<color=#FFB04D>Radiactivo</color>" : "No radiactivo",
                $"Densidad: {e.Density}"
            };
            for (int i = 0; i < chips.Length; i++)
            {
                var c = new Vector3(-size.x / 2 + 0.2f + (size.x - 0.4f) / 4f * (i + 0.5f), -1.42f, 0);
                LabFactory.Card(s, c, new Vector2((size.x - 0.4f) / 4f - 0.08f, 0.42f), new Color(0.1f, 0.24f, 0.2f));
                var t = LabFactory.Text(s, chips[i], c + new Vector3(0, 0, -0.015f), 0.075f, Color.white, TextAlignmentOptions.Center, (size.x - 0.4f) / 4f - 0.16f, 0.36f);
                AutoFit(t, 0.075f, 0.045f);
            }
        }

        // ---------------- Ventana con el paisaje del elemento (pared de atras) ----------------
        void BuildLandscape()
        {
            if (landscapeAnchor == null) return;
            LabFactory.ClearChildren(landscapeAnchor);
            var land = ElementExtras.Landscape(ElementExtras.Sample(element).Landscape);
            var tex = land?.Load();
            if (tex == null) return;
            var m = new Material(LabFactory.Mat("Panorama")) { mainTexture = tex, name = "Paisaje_" + land.Key };
            // Se dibuja al final para que solo cueste lo que se ve por la ventana.
            m.renderQueue = 2450;
            LabFactory.MeshObject("Paisaje360", MeshKit.PanoSphere(), landscapeAnchor, Vector3.zero, Vector3.one * 40f, m);
        }

        static void AutoFit(TextMeshPro t, float max, float min)
        {
            t.enableAutoSizing = true;
            t.fontSizeMax = max * 10f;
            t.fontSizeMin = min * 10f;
            t.overflowMode = TextOverflowModes.Ellipsis;
        }
    }
}

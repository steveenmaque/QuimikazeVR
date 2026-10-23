using TMPro;
using UnityEngine;

namespace LabQuimicaVR
{
    public enum SampleKind { Metal, Crystal, Liquid, Gas, GlowGas, Unseen }

    public struct Appearance
    {
        public SampleKind Kind;
        public Color Color;
        public float Metallic, Smoothness;
        public string Text;
        public bool Glows;
    }

    /// <summary>Como se ve cada elemento en la realidad (aproximado) para la muestra 3D.</summary>
    public static class ElementAppearance
    {
        static Appearance A(SampleKind k, string hex, float metallic, float smooth, string text, bool glows = false) =>
            new Appearance { Kind = k, Color = CategoryInfo.Hex(hex), Metallic = metallic, Smoothness = smooth, Text = text, Glows = glows };

        public static Appearance Get(ElementInfo e)
        {
            switch (e.Symbol)
            {
                case "H": return A(SampleKind.GlowGas, "D98CFF", 0, 0, "Gas incoloro. En un tubo con electricidad brilla rosado violeta.");
                case "He": return A(SampleKind.GlowGas, "FFC2A0", 0, 0, "Gas incoloro. Con electricidad brilla rosa anaranjado.");
                case "N": return A(SampleKind.Gas, "C9D8FF", 0, 0, "Gas incoloro y sin olor.");
                case "O": return A(SampleKind.Gas, "B3D4FF", 0, 0, "Gas incoloro. Al volverse líquido es celeste.");
                case "F": return A(SampleKind.Gas, "F2F28C", 0, 0, "Gas amarillo pálido, muy peligroso.");
                case "Ne": return A(SampleKind.GlowGas, "FF5A26", 0, 0, "Gas incoloro. Con electricidad brilla rojo anaranjado.");
                case "Cl": return A(SampleKind.Gas, "C2F04D", 0, 0, "Gas amarillo verdoso de olor fuerte.");
                case "Ar": return A(SampleKind.GlowGas, "B57EFF", 0, 0, "Gas incoloro. Con electricidad brilla lila.");
                case "Kr": return A(SampleKind.GlowGas, "E2DCFF", 0, 0, "Gas incoloro. Con electricidad brilla blanco lila.");
                case "Xe": return A(SampleKind.GlowGas, "7F9CFF", 0, 0, "Gas incoloro. Con electricidad brilla azul.");
                case "Rn": return A(SampleKind.Gas, "FFB0A0", 0, 0, "Gas incoloro y radiactivo.");
                case "Br": return A(SampleKind.Liquid, "7A1406", 0, 0.8f, "Líquido rojo oscuro que suelta vapores naranjas.");
                case "Hg": return A(SampleKind.Liquid, "D6D8E0", 1, 0.95f, "Metal líquido plateado.");
                case "C": return A(SampleKind.Crystal, "1E1E1E", 0.2f, 0.4f, "Grafito negro (o diamante transparente).");
                case "S": return A(SampleKind.Crystal, "FFE135", 0, 0.3f, "Sólido amarillo quebradizo.");
                case "P": return A(SampleKind.Crystal, "F3EEDB", 0, 0.5f, "Sólido blanco ceroso (fósforo blanco) o rojo.");
                case "I": return A(SampleKind.Crystal, "3A2350", 0.6f, 0.8f, "Cristales gris violeta brillantes.");
                case "Se": return A(SampleKind.Crystal, "5A5558", 0.5f, 0.6f, "Sólido gris metálico.");
                case "B": return A(SampleKind.Crystal, "2E2A27", 0.4f, 0.6f, "Sólido negro brillante y muy duro.");
                case "Si": return A(SampleKind.Crystal, "5E6A80", 0.6f, 0.8f, "Sólido gris azulado brillante.");
                case "As": return A(SampleKind.Crystal, "6E6E72", 0.6f, 0.5f, "Sólido gris metálico.");
                case "Te": return A(SampleKind.Crystal, "9A9CA2", 0.7f, 0.7f, "Sólido plateado y quebradizo.");
                case "Au": return A(SampleKind.Metal, "FFC94D", 1, 0.9f, "Metal amarillo brillante.");
                case "Cu": return A(SampleKind.Metal, "E08B5C", 1, 0.85f, "Metal rojizo brillante.");
                case "Cs": return A(SampleKind.Metal, "EBD08A", 1, 0.8f, "Metal dorado pálido, muy blando.");
                case "Os": return A(SampleKind.Metal, "9DB0C8", 1, 0.85f, "Metal gris azulado.");
                case "Bi": return A(SampleKind.Metal, "D2A8E0", 1, 0.9f, "Metal con cristales de colores arcoíris.");
                case "Ra": return A(SampleKind.Metal, "E8ECF2", 0.8f, 0.6f, "Metal blanco plateado que brilla en la oscuridad.", true);
                case "Ac": return A(SampleKind.Metal, "E8ECF2", 0.8f, 0.6f, "Metal plateado que brilla azul pálido en la oscuridad.", true);
                case "Es": return A(SampleKind.Metal, "F0F0F0", 0.8f, 0.6f, "Metal plateado; se ha visto en cantidades diminutas que brillan.", true);
                case "Fr":
                case "At": return A(SampleKind.Unseen, "FF6B6B", 0, 0, "Nunca se ha reunido suficiente cantidad para verlo a simple vista.");
            }
            if (e.Z >= 100) return A(SampleKind.Unseen, "FF6B6B", 0, 0, "Solo se han creado unos pocos átomos: nadie lo ha visto a simple vista.");
            if (e.IsMetal)
            {
                string t = e.Category switch
                {
                    ElementCategory.AlkaliMetal => "Metal plateado tan blando que se corta con cuchillo.",
                    ElementCategory.AlkalineEarth => "Metal plateado claro.",
                    ElementCategory.Actinide => "Metal plateado y radiactivo.",
                    ElementCategory.Lanthanide => "Metal plateado brillante.",
                    _ => e.IsRadioactive ? "Metal plateado y radiactivo." : "Metal gris plateado brillante."
                };
                return A(SampleKind.Metal, e.IsRadioactive ? "C8D0C8" : "C9CCD2", 1, 0.8f, t);
            }
            return A(SampleKind.Crystal, "9A9CA2", 0.5f, 0.6f, "Sólido gris brillante.");
        }
    }

    /// <summary>Muestra fisica del elemento sobre un pedestal: metal, cristal, liquido o gas en un frasco.</summary>
    public static class ElementSample
    {
        public static void Build(Transform anchor, ElementInfo e)
        {
            var ap = ElementAppearance.Get(e);
            LabFactory.Shape("Pedestal", PrimitiveType.Cylinder, anchor, new Vector3(0, 0.45f, 0), new Vector3(0.5f, 0.45f, 0.5f), LabFactory.Mat("Pedestal"), true);
            LabFactory.Shape("Tapa", PrimitiveType.Cylinder, anchor, new Vector3(0, 0.91f, 0), new Vector3(0.56f, 0.015f, 0.56f), LabFactory.Tinted("GlowBase", Palette.Accent, 1.4f));
            var holder = LabFactory.Node("Muestra", anchor, new Vector3(0, 1.12f, 0)).transform;
            var spin = holder.gameObject.AddComponent<Spin>();
            spin.degreesPerSecond = new Vector3(0, 25, 0);
            spin.bobHeight = 0.02f;

            var mat = LabFactory.Tinted("Sample", ap.Color, ap.Glows ? 0.6f : 0f);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", ap.Metallic);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", ap.Smoothness);

            switch (ap.Kind)
            {
                case SampleKind.Metal:
                    LabFactory.Box("Trozo1", holder, new Vector3(0, 0, 0), new Vector3(0.16f, 0.12f, 0.14f), mat).transform.localRotation = Quaternion.Euler(12, 20, 8);
                    LabFactory.Box("Trozo2", holder, new Vector3(0.05f, 0.04f, -0.02f), new Vector3(0.11f, 0.1f, 0.12f), mat).transform.localRotation = Quaternion.Euler(-25, 50, 30);
                    LabFactory.Box("Trozo3", holder, new Vector3(-0.05f, -0.02f, 0.03f), new Vector3(0.1f, 0.09f, 0.1f), mat).transform.localRotation = Quaternion.Euler(40, -10, -20);
                    break;
                case SampleKind.Crystal:
                    for (int i = 0; i < 5; i++)
                    {
                        float a = i * 72f;
                        var c = LabFactory.Box("Cristal", holder, Quaternion.Euler(0, a, 0) * new Vector3(0.04f, -0.02f, 0) , new Vector3(0.05f, 0.16f - i * 0.015f, 0.05f), mat);
                        c.transform.localRotation = Quaternion.Euler(18 * (i % 2 == 0 ? 1 : -1), a, 15);
                    }
                    break;
                case SampleKind.Liquid:
                case SampleKind.Gas:
                case SampleKind.GlowGas:
                    BuildVial(holder, ap);
                    break;
                case SampleKind.Unseen:
                    var glow = LabFactory.Shape("Atomos", PrimitiveType.Sphere, holder, Vector3.zero, Vector3.one * 0.09f, LabFactory.Tinted("GlowBase", ap.Color, 2f));
                    glow.AddComponent<Spin>().degreesPerSecond = new Vector3(40, 60, 0);
                    LabFactory.Text(holder, "?", new Vector3(0, 0.16f, 0), 0.12f, Palette.Warn, TextAlignmentOptions.Center, 0.3f, 0.2f, true)
                        .gameObject.AddComponent<FaceCamera>();
                    break;
            }

            var label = LabFactory.Node("Etiqueta", anchor, new Vector3(0, 1.62f, 0)).transform;
            label.gameObject.AddComponent<FaceCamera>();
            LabFactory.Box("Fondo", label, new Vector3(0, 0, 0.02f), new Vector3(1.1f, 0.42f, 0.02f), LabFactory.Mat("Screen"));
            LabFactory.Box("Borde", label, new Vector3(0, 0.215f, 0.02f), new Vector3(1.1f, 0.012f, 0.025f), LabFactory.Tinted("GlowBase", Palette.Accent2, 1.5f));
            LabFactory.Text(label, "<b>ASÍ SE VE</b>", new Vector3(0, 0.12f, 0), 0.075f, Palette.Accent2, TextAlignmentOptions.Center, 1.0f, 0.14f);
            var t = LabFactory.Text(label, ap.Text, new Vector3(0, -0.06f, 0), 0.06f, Color.white, TextAlignmentOptions.Center, 1.0f, 0.26f);
            t.enableAutoSizing = true;
            t.fontSizeMax = 0.6f;
            t.fontSizeMin = 0.4f;
        }

        static void BuildVial(Transform holder, Appearance ap)
        {
            var vial = LabFactory.LoadMesh("Vial");
            var glass = LabFactory.Mat("Glass");
            if (vial != null) LabFactory.MeshObject("Frasco", vial, holder, new Vector3(0, -0.12f, 0), Vector3.one * 0.9f, glass);
            else LabFactory.Shape("Frasco", PrimitiveType.Cylinder, holder, new Vector3(0, 0.02f, 0), new Vector3(0.12f, 0.14f, 0.12f), glass);
            LabFactory.Shape("Tapon", PrimitiveType.Cylinder, holder, new Vector3(0, 0.19f, 0), new Vector3(0.07f, 0.02f, 0.07f), LabFactory.Mat("BenchTop"));

            if (ap.Kind == SampleKind.Liquid)
            {
                var liquid = LabFactory.Tinted("Sample", ap.Color);
                if (liquid.HasProperty("_Metallic")) liquid.SetFloat("_Metallic", ap.Metallic);
                if (liquid.HasProperty("_Glossiness")) liquid.SetFloat("_Glossiness", ap.Smoothness);
                LabFactory.Shape("Liquido", PrimitiveType.Cylinder, holder, new Vector3(0, -0.06f, 0), new Vector3(0.095f, 0.05f, 0.095f), liquid);
            }
            else
            {
                float emission = ap.Kind == SampleKind.GlowGas ? 1.8f : 0.15f;
                var gas = LabFactory.Tinted("GasGlow", new Color(ap.Color.r, ap.Color.g, ap.Color.b, ap.Kind == SampleKind.GlowGas ? 0.55f : 0.22f), emission);
                LabFactory.Shape("Gas", PrimitiveType.Cylinder, holder, new Vector3(0, 0.01f, 0), new Vector3(0.095f, 0.12f, 0.095f), gas);
            }
        }
    }
}

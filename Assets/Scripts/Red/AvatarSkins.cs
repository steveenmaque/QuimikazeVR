using System;
using UnityEngine;

namespace LabQuimicaVR
{
    /// <summary>Personajes geométricos propios: modelos reales, sin texturas externas ni colliders.</summary>
    public static class AvatarSkins
    {
        public const string PreferenceKey = "Quimikaze.AvatarSkin.v1";
        static readonly string[] Names = { "Explorador cobalto", "Guardián esmeralda", "Ingeniera solar", "Robot violeta" };
        public static int Count => LanSession.SkinCount;
        public static event Action<int> SelectionChanged;
        public static bool IsValid(int index) => LanSession.IsValidSkin(index);
        public static string Name(int index) => Names[IsValid(index) ? index : 0];
        public static string SelectedName => Name(SelectedIndex);
        public static int SelectedIndex
        {
            get
            {
                int index = PlayerPrefs.GetInt(PreferenceKey, 0);
                return IsValid(index) ? index : 0;
            }
            set
            {
                if (!IsValid(value)) throw new ArgumentOutOfRangeException(nameof(value));
                int previous = SelectedIndex;
                PlayerPrefs.SetInt(PreferenceKey, value);
                PlayerPrefs.Save();
                if (previous != value) SelectionChanged?.Invoke(value);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetEvents() { SelectionChanged = null; }

        /// <summary>Altura aproximada 1.85 m; posición local cero a los pies. El llamador controla escala/rotación.</summary>
        public static Transform BuildPreview(Transform parent, int index)
        {
            if (!IsValid(index)) index = 0;
            var root = LabFactory.Node("Personaje_" + index, parent).transform;
            Color suit = index == 0 ? new Color(0.08f, 0.39f, 0.92f) : index == 1 ? new Color(0.08f, 0.62f, 0.35f)
                : index == 2 ? new Color(1f, 0.45f, 0.09f) : new Color(0.57f, 0.28f, 0.91f);
            Color accent = index == 0 ? Palette.Accent : index == 1 ? new Color(0.65f, 0.95f, 0.25f)
                : index == 2 ? new Color(1f, 0.84f, 0.15f) : new Color(0.23f, 0.91f, 0.96f);
            var body = LabFactory.Tinted("Card", suit);
            var trim = LabFactory.Tinted("Card", accent);
            var dark = LabFactory.Tinted("Card", new Color(0.07f, 0.1f, 0.17f));
            var face = LabFactory.Tinted("Card", index == 3 ? new Color(0.76f, 0.69f, 0.95f) : new Color(0.88f, 0.76f, 0.62f));
            bool robot = index == 3;

            Shape("Torso", robot || index == 2 ? PrimitiveType.Cube : PrimitiveType.Capsule,
                root, new Vector3(0, 0.87f, 0), new Vector3(0.43f, robot || index == 2 ? 0.55f : 0.37f, 0.3f), body);
            for (int side = -1; side <= 1; side += 2)
            {
                Shape("Bota", PrimitiveType.Cube, root, new Vector3(side * 0.13f, 0.13f, 0.03f), new Vector3(0.2f, 0.22f, 0.32f), dark);
                Shape("Pierna", PrimitiveType.Capsule, root, new Vector3(side * 0.13f, 0.42f, 0), new Vector3(0.15f, 0.2f, 0.15f), body);
                Shape("Brazo", PrimitiveType.Capsule, root, new Vector3(side * 0.3f, 0.88f, 0), new Vector3(0.13f, 0.25f, 0.13f), body);
                Shape("Guante", PrimitiveType.Sphere, root, new Vector3(side * 0.3f, 0.64f, 0), Vector3.one * 0.15f, trim);
            }
            Shape("Cabeza", robot ? PrimitiveType.Cube : PrimitiveType.Sphere, root, new Vector3(0, 1.43f, 0), Vector3.one * 0.36f, face);
            Shape("Visor", PrimitiveType.Cube, root, new Vector3(0, 1.47f, 0.174f), new Vector3(0.27f, 0.1f, 0.045f), dark);
            Shape("LuzVisor", PrimitiveType.Cube, root, new Vector3(0, 1.47f, 0.2f), new Vector3(0.19f, 0.035f, 0.013f), trim);
            Shape("Insignia", PrimitiveType.Sphere, root, new Vector3(0, 0.96f, 0.165f), Vector3.one * 0.12f, trim);

            if (index == 0)
            {
                Shape("CascoEspacial", PrimitiveType.Sphere, root, new Vector3(0, 1.58f, -0.02f), new Vector3(0.4f, 0.2f, 0.4f), body);
                Shape("MochilaExploracion", PrimitiveType.Cube, root, new Vector3(0, 0.94f, -0.24f), new Vector3(0.3f, 0.48f, 0.18f), trim);
                Shape("Antena", PrimitiveType.Cylinder, root, new Vector3(0.18f, 1.72f, -0.02f), new Vector3(0.025f, 0.15f, 0.025f), dark);
                Shape("AntenaPunta", PrimitiveType.Sphere, root, new Vector3(0.18f, 1.86f, -0.02f), Vector3.one * 0.07f, trim);
            }
            else if (index == 1)
            {
                Shape("GorroBotanico", PrimitiveType.Cylinder, root, new Vector3(0, 1.6f, 0), new Vector3(0.43f, 0.035f, 0.43f), trim);
                Shape("DepositoVerde", PrimitiveType.Cylinder, root, new Vector3(0, 0.94f, -0.24f), new Vector3(0.25f, 0.3f, 0.25f), trim);
                var leaf = Shape("HojaIzquierda", PrimitiveType.Sphere, root, new Vector3(-0.1f, 1.77f, 0), new Vector3(0.13f, 0.3f, 0.055f), trim);
                leaf.transform.localRotation = Quaternion.Euler(0, 0, 35);
                leaf = Shape("HojaDerecha", PrimitiveType.Sphere, root, new Vector3(0.1f, 1.77f, 0), new Vector3(0.13f, 0.3f, 0.055f), body);
                leaf.transform.localRotation = Quaternion.Euler(0, 0, -35);
            }
            else if (index == 2)
            {
                Shape("CascoSeguridad", PrimitiveType.Sphere, root, new Vector3(0, 1.59f, -0.01f), new Vector3(0.42f, 0.22f, 0.43f), trim);
                Shape("Visera", PrimitiveType.Cube, root, new Vector3(0, 1.58f, 0.18f), new Vector3(0.45f, 0.05f, 0.2f), trim);
                Shape("Cinturon", PrimitiveType.Cube, root, new Vector3(0, 0.68f, 0), new Vector3(0.46f, 0.1f, 0.34f), dark);
                Shape("CajaHerramientas", PrimitiveType.Cube, root, new Vector3(0.28f, 0.6f, 0), new Vector3(0.18f, 0.23f, 0.2f), trim);
            }
            else
            {
                Shape("AntenaRobot", PrimitiveType.Cylinder, root, new Vector3(0, 1.72f, 0), new Vector3(0.035f, 0.11f, 0.035f), dark);
                Shape("AntenaOrbita", PrimitiveType.Sphere, root, new Vector3(0, 1.86f, 0), Vector3.one * 0.12f, trim);
                Shape("PanelRobot", PrimitiveType.Cube, root, new Vector3(0, 0.88f, 0.17f), new Vector3(0.26f, 0.27f, 0.035f), dark);
                for (int row = 0; row < 3; row++) Shape("Indicador", PrimitiveType.Cube, root, new Vector3(0, 0.81f + row * 0.07f, 0.2f), new Vector3(0.17f, 0.025f, 0.015f), trim);
            }
            return root;
        }

        static GameObject Shape(string name, PrimitiveType shape, Transform parent, Vector3 position, Vector3 scale, Material material)
            => LabFactory.Shape(name, shape, parent, position, scale, material, false);
    }
}

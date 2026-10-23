using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace LabQuimicaVR
{
    public enum AtomViewMode { Bohr, Cloud, Nucleus }

    /// <summary>
    /// Modelo 3D del atomo: nucleo con protones y neutrones, niveles de energia (modelo de Bohr)
    /// con sus electrones girando, nube electronica (orbitales s, p, d, f) y vista ampliada del nucleo.
    /// </summary>
    public class AtomModel : MonoBehaviour
    {
        public const int MaxNucleons = 120;
        public const float NucleonRadius = 0.05f;

        public AtomViewMode Mode { get; private set; } = AtomViewMode.Bohr;

        Transform nucleus, shellsRoot, cloudRoot;
        readonly List<(Transform electrons, float speed)> shells = new List<(Transform, float)>();
        ElectronCloud cloud;
        ElementInfo element;
        float nucleusScale = 1f;

        public static float ShellRadius(int n) => 0.3f + 0.17f * n;

        static Mesh sphereMesh;

        /// <summary>Esfera de pocos poligonos (120 triangulos) para protones, neutrones y electrones.</summary>
        static Mesh SphereMesh
        {
            get
            {
                if (sphereMesh == null) sphereMesh = LabFactory.LoadMesh("EsferaBaja");
                if (sphereMesh == null) sphereMesh = LabFactory.Primitive(PrimitiveType.Sphere);
                return sphereMesh;
            }
        }

        public void Build(ElementInfo e)
        {
            element = e;
            LabFactory.ClearChildren(transform);
            shells.Clear();

            nucleus = LabFactory.Node("Nucleo", transform).transform;
            BuildNucleus(e);

            shellsRoot = LabFactory.Node("Niveles", transform).transform;
            for (int i = 0; i < e.Shells.Length; i++) BuildShell(i + 1, e.Shells[i]);

            cloudRoot = LabFactory.Node("NubeElectronica", transform).transform;
            cloud = cloudRoot.gameObject.AddComponent<ElectronCloud>();

            SetMode(Mode);
        }

        void BuildNucleus(ElementInfo e)
        {
            int total = e.Z + e.Neutrons;
            int count = Mathf.Min(total, MaxNucleons);
            int protons = Mathf.Max(1, Mathf.RoundToInt(count * (e.Z / (float)total)));
            float radius = NucleonRadius * Mathf.Pow(count, 1f / 3f) * 1.15f;
            var rng = new System.Random(e.Z * 31 + 7);

            // Mezcla determinista de protones y neutrones
            var isProton = new bool[count];
            for (int i = 0; i < protons; i++) isProton[i] = true;
            for (int i = count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (isProton[i], isProton[j]) = (isProton[j], isProton[i]);
            }

            var protonMat = LabFactory.Mat("Proton");
            var neutronMat = LabFactory.Mat("Neutron");
            float golden = Mathf.PI * (3f - Mathf.Sqrt(5f));
            for (int i = 0; i < count; i++)
            {
                float r = count == 1 ? 0f : radius * Mathf.Pow((i + 0.5f) / count, 1f / 3f);
                float y = 1f - 2f * (i + 0.5f) / count;
                float ring = Mathf.Sqrt(Mathf.Max(0f, 1f - y * y));
                float theta = golden * i;
                var dir = new Vector3(Mathf.Cos(theta) * ring, y, Mathf.Sin(theta) * ring);
                LabFactory.MeshObject(isProton[i] ? "Proton" : "Neutron", SphereMesh, nucleus, dir * r,
                    Vector3.one * NucleonRadius * 2.1f, isProton[i] ? protonMat : neutronMat);
            }
        }

        void BuildShell(int n, int electrons)
        {
            float radius = ShellRadius(n);
            var shell = LabFactory.Node($"Nivel_{ElectronConfiguration.ShellLetters[n - 1]}", shellsRoot,
                Vector3.zero, Quaternion.Euler(((n * 37) % 50) - 25f, n * 53f, ((n * 23) % 30) - 15f)).transform;

            var color = Color.Lerp(new Color(0.3f, 0.85f, 1f), new Color(0.75f, 0.45f, 1f), (n - 1) / 6f);
            LabFactory.Circle("Orbita", shell, radius, 0.012f, new Color(color.r, color.g, color.b, 0.75f), 96);

            var electronsRoot = LabFactory.Node("Electrones", shell).transform;
            var mat = LabFactory.Mat("Electron");
            for (int i = 0; i < electrons; i++)
            {
                float a = i * Mathf.PI * 2f / electrons;
                LabFactory.MeshObject("Electron", SphereMesh, electronsRoot,
                    new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius), Vector3.one * 0.075f, mat);
            }

            var label = LabFactory.Text(shell, $"<b>{ElectronConfiguration.ShellLetters[n - 1]}</b> {electrons}e<sup>-</sup>",
                new Vector3(radius + 0.12f, 0f, 0f), 0.085f, color, TextAlignmentOptions.Center, 0.6f, 0.2f);
            label.gameObject.AddComponent<FaceCamera>();

            shells.Add((electronsRoot, 70f / (n + 0.5f)));
        }

        public void SetMode(AtomViewMode mode)
        {
            Mode = mode;
            if (shellsRoot == null) return;
            shellsRoot.gameObject.SetActive(mode == AtomViewMode.Bohr);
            cloudRoot.gameObject.SetActive(mode == AtomViewMode.Cloud);
            nucleusScale = mode == AtomViewMode.Nucleus ? 4.5f : mode == AtomViewMode.Cloud ? 0.3f : 1f;
            if (element != null && element.Shells.Length > 0)
            {
                int n = element.Shells.Length;
                cloudRoot.localScale = Vector3.one * (ShellRadius(n) / ElectronCloud.CloudRadius(n));
            }
            if (!Application.isPlaying) nucleus.localScale = Vector3.one * nucleusScale;
            if (mode == AtomViewMode.Cloud && Application.isPlaying && element != null) cloud.Generate(element);
        }

        public AtomViewMode NextMode() => (AtomViewMode)(((int)Mode + 1) % 3);

        void Update()
        {
            if (!Application.isPlaying || shellsRoot == null) return;
            float dt = Time.deltaTime;
            foreach (var (electrons, speed) in shells) electrons.Rotate(0f, speed * dt, 0f, Space.Self);
            shellsRoot.Rotate(0f, 6f * dt, 0f, Space.World);
            nucleus.Rotate(0f, 12f * dt, 0f, Space.World);
            nucleus.localScale = Vector3.Lerp(nucleus.localScale, Vector3.one * nucleusScale, dt * 4f);
        }
    }
}

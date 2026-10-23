using System;
using System.Collections.Generic;
using UnityEngine;

namespace LabQuimicaVR
{
    /// <summary>
    /// Nube electronica: miles de puntos repartidos segun la forma de los orbitales ocupados
    /// (s esfericos, p con dos lobulos, d con cuatro, f con mas). Los puntos "parpadean" para
    /// recordar que no sabemos exactamente donde esta cada electron.
    /// </summary>
    public class ElectronCloud : MonoBehaviour
    {
        /// <summary>Menos puntos en el celular: se dibujan dos veces (un ojo cada vez).</summary>
        public static int MaxPoints => Application.isMobilePlatform ? 1800 : 4200;

        static readonly Color[] SubshellColors =
        {
            new Color(0.25f, 0.8f, 1f, 0.7f),   // s
            new Color(0.3f, 1f, 0.45f, 0.7f),   // p
            new Color(1f, 0.65f, 0.15f, 0.75f), // d
            new Color(1f, 0.35f, 0.75f, 0.75f)  // f
        };

        // Funciones angulares (sin normalizar) de los orbitales reales.
        static readonly Func<Vector3, float>[][] Angular =
        {
            new Func<Vector3, float>[] { v => 1f },
            new Func<Vector3, float>[] { v => v.x, v => v.y, v => v.z },
            new Func<Vector3, float>[] { v => v.x * v.y, v => v.y * v.z, v => v.x * v.z, v => v.x * v.x - v.y * v.y, v => 3f * v.z * v.z - 1f },
            new Func<Vector3, float>[]
            {
                v => v.z * (5f * v.z * v.z - 3f), v => v.x * (5f * v.z * v.z - 1f), v => v.y * (5f * v.z * v.z - 1f),
                v => v.z * (v.x * v.x - v.y * v.y), v => v.x * v.y * v.z, v => v.x * (v.x * v.x - 3f * v.y * v.y),
                v => v.y * (3f * v.x * v.x - v.y * v.y)
            }
        };

        static float[][] maxSquared;

        ParticleSystem ps;
        ParticleSystem.Particle[] particles;
        ParticleSystem.Particle[] scratch;
        int cursor;
        readonly List<(int n, int l, int k)> sources = new List<(int, int, int)>();
        System.Random rng = new System.Random(1);
        int count;

        static void EnsureMax()
        {
            if (maxSquared != null) return;
            var r = new System.Random(3);
            maxSquared = new float[4][];
            for (int l = 0; l < 4; l++)
            {
                maxSquared[l] = new float[Angular[l].Length];
                for (int k = 0; k < Angular[l].Length; k++)
                {
                    float max = 0f;
                    for (int i = 0; i < 6000; i++)
                    {
                        var d = RandomDir(r);
                        float a = Angular[l][k](d);
                        max = Mathf.Max(max, a * a);
                    }
                    maxSquared[l][k] = max * 1.05f;
                }
            }
        }

        static Vector3 RandomDir(System.Random r)
        {
            float z = (float)(r.NextDouble() * 2 - 1);
            float t = (float)(r.NextDouble() * Math.PI * 2);
            float s = Mathf.Sqrt(1 - z * z);
            return new Vector3(s * Mathf.Cos(t), z, s * Mathf.Sin(t));
        }

        static float Gaussian(System.Random r)
        {
            double u1 = 1.0 - r.NextDouble(), u2 = r.NextDouble();
            return (float)(Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2));
        }

        public static float CloudRadius(int n) => 0.12f * Mathf.Pow(n, 1.25f) + 0.06f;

        void EnsureSystem()
        {
            if (ps != null) return;
            ps = gameObject.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.maxParticles = MaxPoints;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = 100000f;
            main.startSpeed = 0f;
            var emission = ps.emission;
            emission.enabled = false;
            var shape = ps.shape;
            shape.enabled = false;
            var r = GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = LabFactory.Mat("CloudParticle");
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.sortMode = ParticleSystemSortMode.None;
            r.maxParticleSize = 0.05f;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        public void Generate(ElementInfo e)
        {
            EnsureMax();
            EnsureSystem();
            rng = new System.Random(e.Z);
            sources.Clear();

            // Reparte los electrones de cada subnivel en sus orbitales (regla de Hund).
            foreach (var o in e.Orbitals)
            {
                int m = 2 * o.L + 1;
                for (int k = 0; k < m; k++)
                {
                    int inOrbital = o.Electrons <= m ? (k < o.Electrons ? 1 : 0) : (k < o.Electrons - m ? 2 : 1);
                    for (int i = 0; i < inOrbital; i++) sources.Add((o.N, o.L, k));
                }
            }
            int perElectron = Mathf.Clamp(MaxPoints / Mathf.Max(1, sources.Count), 25, 600);
            count = Mathf.Min(MaxPoints, perElectron * sources.Count);
            particles = new ParticleSystem.Particle[count];
            for (int i = 0; i < count; i++) particles[i] = MakeParticle(sources[i % sources.Count]);
            ps.SetParticles(particles, count);
            ps.Play();
            scratch = new ParticleSystem.Particle[Mathf.Max(1, count / 40)];
            cursor = 0;
        }

        ParticleSystem.Particle MakeParticle((int n, int l, int k) src)
        {
            var fn = Angular[src.l][src.k];
            float maxSq = maxSquared[src.l][src.k];
            Vector3 dir = RandomDir(rng);
            for (int tries = 0; tries < 40; tries++)
            {
                float a = fn(dir);
                if (rng.NextDouble() * maxSq <= a * a) break;
                dir = RandomDir(rng);
            }
            float radial = Mathf.Clamp(1f + Gaussian(rng) * 0.28f, 0.25f, 1.9f);
            if (src.l == 0) radial *= Mathf.Lerp(0.6f, 1f, (float)rng.NextDouble());
            var c = SubshellColors[src.l];
            c.a *= Mathf.Lerp(0.5f, 1f, src.n / 7f);
            return new ParticleSystem.Particle
            {
                position = dir * CloudRadius(src.n) * radial,
                startColor = c,
                startSize = 0.028f,
                remainingLifetime = 100000f,
                startLifetime = 100000f
            };
        }

        void Update()
        {
            if (ps == null || particles == null || scratch == null || count == 0) return;
            transform.Rotate(0f, 10f * Time.deltaTime, 0f, Space.World);
            // Parpadeo: cada 2 cuadros se recoloca un bloque de puntos. Se sube el arreglo completo porque
            // SetParticles con desplazamiento deja el sistema con (desplazamiento + tamano) particulas.
            if ((Time.frameCount & 1) != 0) return;
            int block = scratch.Length * 2;
            for (int i = 0; i < block; i++)
            {
                int idx = (cursor + i) % count;
                particles[idx] = MakeParticle(sources[idx % sources.Count]);
            }
            cursor = (cursor + block) % count;
            ps.SetParticles(particles, count);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace LabQuimicaVR
{
    /// <summary>
    /// Mallas procedurales para las muestras de los elementos: rocas facetadas, cristales, barras
    /// cristalinas, escamas, pepitas, cristales de bismuto, ampollas de vidrio, gotas, polvo...
    /// Todo es deterministico (misma semilla = misma forma) y se genera en tiempo de ejecucion.
    /// </summary>
    public static class MeshKit
    {
        // ------------------------------------------------------------------ constructor de mallas
        public class Builder
        {
            public readonly List<Vector3> V = new List<Vector3>();
            public readonly List<Vector3> N = new List<Vector3>();
            public readonly List<Vector2> UV = new List<Vector2>();
            public readonly List<int> T = new List<int>();

            /// <summary>Si tiene valor, cada cara plana se orienta para mirar en direccion contraria a este punto.</summary>
            public Vector3? OutFrom;

            /// <summary>Triangulo con sombreado plano (caras de cristal).</summary>
            public void FlatTri(Vector3 a, Vector3 b, Vector3 c)
            {
                var n = Vector3.Cross(b - a, c - a);
                if (n.sqrMagnitude < 1e-14f) return;
                if (OutFrom.HasValue && Vector3.Dot(n, (a + b + c) / 3f - OutFrom.Value) < 0f)
                {
                    var tmp = b; b = c; c = tmp;
                    n = -n;
                }
                n.Normalize();
                int i = V.Count;
                V.Add(a); V.Add(b); V.Add(c);
                N.Add(n); N.Add(n); N.Add(n);
                UV.Add(new Vector2(a.x, a.y)); UV.Add(new Vector2(b.x, b.y)); UV.Add(new Vector2(c.x, c.y));
                T.Add(i); T.Add(i + 1); T.Add(i + 2);
            }

            public void FlatQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d) { FlatTri(a, b, c); FlatTri(a, c, d); }

            public int Vert(Vector3 p, Vector3 n, Vector2 uv) { V.Add(p); N.Add(n); UV.Add(uv); return V.Count - 1; }

            public void Tri(int a, int b, int c) { T.Add(a); T.Add(b); T.Add(c); }

            /// <summary>Agrega otra malla transformada.</summary>
            public void Append(Mesh m, Matrix4x4 tr)
            {
                int o = V.Count;
                var vs = m.vertices; var ns = m.normals; var uvs = m.uv;
                var nt = tr.inverse.transpose;
                for (int i = 0; i < vs.Length; i++)
                {
                    V.Add(tr.MultiplyPoint3x4(vs[i]));
                    N.Add(ns.Length > i ? nt.MultiplyVector(ns[i]).normalized : Vector3.up);
                    UV.Add(uvs.Length > i ? uvs[i] : Vector2.zero);
                }
                foreach (var t in m.triangles) T.Add(o + t);
            }

            public Mesh ToMesh(string name)
            {
                var m = new Mesh { name = name };
                if (V.Count > 65000) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                m.SetVertices(V);
                m.SetNormals(N);
                m.SetUVs(0, UV);
                m.SetTriangles(T, 0);
                m.RecalculateBounds();
                return m;
            }
        }

        // ------------------------------------------------------------------ ruido
        static float Hash(int x, int y, int z, int seed)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + z * 1274126177 + seed * 1442695041;
                h = (h ^ (h >> 13)) * 1274126177;
                return ((h ^ (h >> 16)) & 0xFFFFFF) / (float)0xFFFFFF;
            }
        }

        /// <summary>Ruido 3D suave entre 0 y 1.</summary>
        public static float Noise(Vector3 p, int seed)
        {
            int x = Mathf.FloorToInt(p.x), y = Mathf.FloorToInt(p.y), z = Mathf.FloorToInt(p.z);
            float fx = p.x - x, fy = p.y - y, fz = p.z - z;
            fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy); fz = fz * fz * (3 - 2 * fz);
            float L(int dx, int dy, int dz) => Hash(x + dx, y + dy, z + dz, seed);
            float x00 = Mathf.Lerp(L(0, 0, 0), L(1, 0, 0), fx), x10 = Mathf.Lerp(L(0, 1, 0), L(1, 1, 0), fx);
            float x01 = Mathf.Lerp(L(0, 0, 1), L(1, 0, 1), fx), x11 = Mathf.Lerp(L(0, 1, 1), L(1, 1, 1), fx);
            return Mathf.Lerp(Mathf.Lerp(x00, x10, fy), Mathf.Lerp(x01, x11, fy), fz);
        }

        static float Fbm(Vector3 p, int seed, int octaves)
        {
            float sum = 0, amp = 0.5f, norm = 0;
            for (int i = 0; i < octaves; i++)
            {
                sum += Noise(p, seed + i * 17) * amp;
                norm += amp;
                amp *= 0.5f;
                p *= 2.03f;
            }
            return sum / norm;
        }

        // ------------------------------------------------------------------ icosfera
        static void Icosphere(int subdivisions, out List<Vector3> verts, out List<int> tris)
        {
            float t = (1f + Mathf.Sqrt(5f)) / 2f;
            verts = new List<Vector3>
            {
                new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
                new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
                new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1)
            };
            for (int i = 0; i < verts.Count; i++) verts[i] = verts[i].normalized;
            tris = new List<int> { 0,11,5, 0,5,1, 0,1,7, 0,7,10, 0,10,11, 1,5,9, 5,11,4, 11,10,2, 10,7,6, 7,1,8,
                                   3,9,4, 3,4,2, 3,2,6, 3,6,8, 3,8,9, 4,9,5, 2,4,11, 6,2,10, 8,6,7, 9,8,1 };
            for (int s = 0; s < subdivisions; s++)
            {
                var cache = new Dictionary<long, int>();
                var vs = verts;
                int Mid(int a, int b)
                {
                    long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
                    if (cache.TryGetValue(key, out var idx)) return idx;
                    vs.Add(((vs[a] + vs[b]) * 0.5f).normalized);
                    return cache[key] = vs.Count - 1;
                }
                var nt = new List<int>(tris.Count * 4);
                for (int i = 0; i < tris.Count; i += 3)
                {
                    int a = tris[i], b = tris[i + 1], c = tris[i + 2];
                    int ab = Mid(a, b), bc = Mid(b, c), ca = Mid(c, a);
                    nt.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
                }
                tris = nt;
            }
        }

        /// <summary>
        /// Roca o trozo irregular. flat = caras planas (cristalino/quebradizo); si no, superficie suave
        /// (pepita, gota). "cut" corta la roca con un plano liso (cara recien cortada del sodio, etc.).
        /// </summary>
        public static Mesh Rock(int seed, Vector3 scale, float roughness, int subdiv, bool flat, float facetJitter = 0.25f, bool cut = false)
        {
            Icosphere(subdiv, out var vs, out var ts);
            var rnd = new System.Random(seed);
            var offset = new Vector3((float)rnd.NextDouble() * 50, (float)rnd.NextDouble() * 50, (float)rnd.NextDouble() * 50);
            // Algunas "caras de fractura": planos que aplanan la roca.
            int planes = flat ? 5 + rnd.Next(4) : 0;
            var planeN = new Vector3[planes];
            var planeD = new float[planes];
            for (int i = 0; i < planes; i++)
            {
                planeN[i] = Random3(rnd).normalized;
                planeD[i] = 0.62f + (float)rnd.NextDouble() * 0.3f;
            }
            Vector3 cutN = new Vector3(0.3f, 1f, 0.2f).normalized;
            for (int i = 0; i < vs.Count; i++)
            {
                var p = vs[i];
                float n = Fbm(p * 1.7f + offset, seed, 3) - 0.5f;
                float r = 1f + n * roughness * 2f;
                if (flat) r += ((float)rnd.NextDouble() - 0.5f) * facetJitter * roughness;
                p *= r;
                for (int k = 0; k < planes; k++)
                {
                    float d = Vector3.Dot(p, planeN[k]);
                    if (d > planeD[k]) p -= planeN[k] * (d - planeD[k]);
                }
                if (cut)
                {
                    float d = Vector3.Dot(p, cutN);
                    if (d > 0.35f) p -= cutN * (d - 0.35f);
                }
                vs[i] = Vector3.Scale(p, scale);
            }
            return Finish(vs, ts, flat, "Roca");
        }

        static Vector3 Random3(System.Random r) =>
            new Vector3((float)r.NextDouble() * 2 - 1, (float)r.NextDouble() * 2 - 1, (float)r.NextDouble() * 2 - 1);

        static Mesh Finish(List<Vector3> vs, List<int> ts, bool flat, string name)
        {
            var b = new Builder { OutFrom = Vector3.zero };
            if (flat)
            {
                for (int i = 0; i < ts.Count; i += 3) b.FlatTri(vs[ts[i]], vs[ts[i + 1]], vs[ts[i + 2]]);
                return b.ToMesh(name);
            }
            OrientOutward(vs, ts, Vector3.zero);
            var m = new Mesh { name = name };
            m.SetVertices(vs);
            m.SetTriangles(ts, 0);
            m.RecalculateNormals();
            var uv = new Vector2[vs.Count];
            for (int i = 0; i < vs.Count; i++) uv[i] = new Vector2(vs[i].x + vs[i].z, vs[i].y);
            m.uv = uv;
            m.RecalculateBounds();
            return m;
        }

        /// <summary>Invierte los triangulos que miran hacia "center" (solidos aproximadamente convexos).</summary>
        static void OrientOutward(List<Vector3> vs, List<int> ts, Vector3 center)
        {
            for (int i = 0; i < ts.Count; i += 3)
            {
                Vector3 a = vs[ts[i]], b = vs[ts[i + 1]], c = vs[ts[i + 2]];
                if (Vector3.Dot(Vector3.Cross(b - a, c - a), (a + b + c) / 3f - center) < 0f)
                {
                    int t = ts[i + 1]; ts[i + 1] = ts[i + 2]; ts[i + 2] = t;
                }
            }
        }

        /// <summary>Gota o nodulo liso y abultado (mercurio, berilio, niquel, plomo).</summary>
        public static Mesh Blob(int seed, Vector3 scale, float lumpiness) => Rock(seed, scale, lumpiness, 3, false);

        // ------------------------------------------------------------------ cristales
        /// <summary>Prisma de n lados terminado en punta (cristal). Va a lo largo de +Y desde el origen.</summary>
        public static void Crystal(Builder b, Matrix4x4 tr, int sides, float radius, float length, float tip, float taper = 0.85f)
        {
            var ring0 = new Vector3[sides];
            var ring1 = new Vector3[sides];
            for (int i = 0; i < sides; i++)
            {
                float a = i * Mathf.PI * 2f / sides;
                var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                ring0[i] = tr.MultiplyPoint3x4(d * radius);
                ring1[i] = tr.MultiplyPoint3x4(d * radius * taper + Vector3.up * length);
            }
            var apex = tr.MultiplyPoint3x4(Vector3.up * (length + tip));
            var bottom = tr.MultiplyPoint3x4(Vector3.zero);
            var saved = b.OutFrom;
            b.OutFrom = tr.MultiplyPoint3x4(Vector3.up * (length * 0.5f));
            for (int i = 0; i < sides; i++)
            {
                int j = (i + 1) % sides;
                b.FlatQuad(ring0[i], ring1[i], ring1[j], ring0[j]);
                b.FlatTri(ring1[i], apex, ring1[j]);
                b.FlatTri(ring0[j], bottom, ring0[i]);
            }
            b.OutFrom = saved;
        }

        /// <summary>Caja con aristas biseladas (cubo pulido de 1 cm, lingotes).</summary>
        public static Mesh BeveledBox(Vector3 size, float bevel)
        {
            var b = new Builder { OutFrom = Vector3.zero };
            Vector3 h = size * 0.5f;
            bevel = Mathf.Min(bevel, Mathf.Min(h.x, Mathf.Min(h.y, h.z)) * 0.9f);
            var corners = new List<Vector3>();
            // 24 vertices: cada esquina tiene 3 puntos desplazados hacia adentro en dos ejes.
            Vector3 P(int sx, int sy, int sz, int axis)
            {
                var p = new Vector3(sx * h.x, sy * h.y, sz * h.z);
                if (axis != 0) p.x -= sx * bevel;
                if (axis != 1) p.y -= sy * bevel;
                if (axis != 2) p.z -= sz * bevel;
                return p;
            }
            int[] s = { -1, 1 };
            // Caras principales
            foreach (int sx in s)
                b.FlatQuad(P(sx, -1, -1, 0), P(sx, 1, -1, 0), P(sx, 1, 1, 0), P(sx, -1, 1, 0));
            foreach (int sy in s)
                b.FlatQuad(P(-1, sy, -1, 1), P(-1, sy, 1, 1), P(1, sy, 1, 1), P(1, sy, -1, 1));
            foreach (int sz in s)
                b.FlatQuad(P(-1, -1, sz, 2), P(1, -1, sz, 2), P(1, 1, sz, 2), P(-1, 1, sz, 2));
            // Biseles de las aristas
            foreach (int sx in s) foreach (int sy in s)
                    b.FlatQuad(P(sx, sy, -1, 0), P(sx, sy, -1, 1), P(sx, sy, 1, 1), P(sx, sy, 1, 0));
            foreach (int sx in s) foreach (int sz in s)
                    b.FlatQuad(P(sx, -1, sz, 0), P(sx, -1, sz, 2), P(sx, 1, sz, 2), P(sx, 1, sz, 0));
            foreach (int sy in s) foreach (int sz in s)
                    b.FlatQuad(P(-1, sy, sz, 1), P(-1, sy, sz, 2), P(1, sy, sz, 2), P(1, sy, sz, 1));
            // Esquinas
            foreach (int sx in s) foreach (int sy in s) foreach (int sz in s)
                        b.FlatTri(P(sx, sy, sz, 0), P(sx, sy, sz, 1), P(sx, sy, sz, 2));
            return b.ToMesh("CajaBiselada");
        }

        /// <summary>Racimo de cristales que crecen desde una base (magnesio, escandio, cromo, plata, oro...).</summary>
        public static Mesh CrystalCluster(int seed, int count, float size, bool branches, int sides = 6)
        {
            var b = new Builder();
            var rnd = new System.Random(seed);
            // Base: roca pequena aplanada
            b.Append(Rock(seed + 1, new Vector3(size * 0.55f, size * 0.22f, size * 0.42f), 0.25f, 1, true), Matrix4x4.identity);
            for (int i = 0; i < count; i++)
            {
                var start = new Vector3(((float)rnd.NextDouble() - 0.5f) * size * 0.8f, 0.02f, ((float)rnd.NextDouble() - 0.5f) * size * 0.6f);
                var dir = (Vector3.up * 1.2f + Random3(rnd) * 0.9f).normalized;
                float len = size * (0.25f + (float)rnd.NextDouble() * 0.5f);
                float rad = size * (0.035f + (float)rnd.NextDouble() * 0.05f);
                var rot = Quaternion.FromToRotation(Vector3.up, dir) * Quaternion.Euler(0, (float)rnd.NextDouble() * 360f, 0);
                Crystal(b, Matrix4x4.TRS(start, rot, Vector3.one), sides, rad, len, rad * 1.4f);
                if (branches)
                {
                    // Ramas laterales (dendritas): cristales mas pequenos a lo largo del principal.
                    int nb = 2 + rnd.Next(3);
                    for (int k = 0; k < nb; k++)
                    {
                        float tpos = 0.3f + (float)rnd.NextDouble() * 0.6f;
                        var p = start + dir * len * tpos;
                        var d2 = (dir * 0.4f + Random3(rnd)).normalized;
                        var r2 = Quaternion.FromToRotation(Vector3.up, d2);
                        Crystal(b, Matrix4x4.TRS(p, r2, Vector3.one), sides, rad * 0.6f, len * 0.4f, rad);
                    }
                }
            }
            return b.ToMesh("Racimo");
        }

        /// <summary>
        /// Masa metalica cristalina: una base irregular cubierta de muchos cristalitos pequenos y
        /// apretados (escandio, cromo, cinc, itrio, molibdeno, magnesio sublimado...).
        /// </summary>
        public static Mesh CrystalMass(int seed, Vector3 size, int crystallites, float grain)
        {
            var b = new Builder();
            var rnd = new System.Random(seed);
            b.Append(Rock(seed, size * 0.5f, 0.3f, 2, true, 0.4f), Matrix4x4.identity);
            for (int i = 0; i < crystallites; i++)
            {
                // Punto sobre la superficie del elipsoide (mas en la parte de arriba)
                var dir = Random3(rnd);
                dir.y = Mathf.Abs(dir.y) * 1.3f + 0.1f;
                dir.Normalize();
                var p = Vector3.Scale(dir, size * 0.5f) * (0.82f + (float)rnd.NextDouble() * 0.2f);
                float g = grain * (0.5f + (float)rnd.NextDouble());
                var rot = Quaternion.Euler((float)rnd.NextDouble() * 360f, (float)rnd.NextDouble() * 360f, (float)rnd.NextDouble() * 360f);
                if (rnd.NextDouble() < 0.35)
                    b.Append(BeveledBox(Vector3.one * g * 1.3f, g * 0.1f), Matrix4x4.TRS(p, rot, Vector3.one));
                else
                    b.Append(Rock(seed + i * 31, new Vector3(g, g * 0.8f, g), 0.35f, 0, true, 0.8f), Matrix4x4.TRS(p, rot, Vector3.one));
            }
            return b.ToMesh("MasaCristalina");
        }

        /// <summary>
        /// Dendrita: ramas finas hechas de cristalitos encadenados, como un helecho
        /// (cobre nativo, cristales de plata y de oro).
        /// </summary>
        public static Mesh Dendrite(int seed, float size, int mainBranches = 5)
        {
            var b = new Builder();
            var rnd = new System.Random(seed);
            void Branch(Vector3 start, Vector3 dir, float length, float thick, int depth)
            {
                int steps = Mathf.Max(3, Mathf.RoundToInt(length / (thick * 1.4f)));
                var p = start;
                for (int s = 0; s < steps; s++)
                {
                    float t = s / (float)steps;
                    float g = thick * (1f - t * 0.6f);
                    var rot = Quaternion.LookRotation(dir) * Quaternion.Euler(45, 45, (float)rnd.NextDouble() * 90f);
                    b.Append(Rock(seed + s * 7 + depth * 101, new Vector3(g, g * 0.75f, g * 1.2f), 0.3f, 0, true, 0.7f), Matrix4x4.TRS(p, rot, Vector3.one));
                    // Ramitas laterales a ~60 grados, casi en el mismo plano
                    if (depth < 2 && s > 0 && rnd.NextDouble() < 0.45)
                    {
                        float side = rnd.NextDouble() < 0.5 ? -1f : 1f;
                        var d2 = Quaternion.AngleAxis(side * (50f + (float)rnd.NextDouble() * 20f), Vector3.up) * dir;
                        d2.y += ((float)rnd.NextDouble() - 0.3f) * 0.4f;
                        Branch(p, d2.normalized, length * (0.35f + (float)rnd.NextDouble() * 0.25f) * (1f - t), thick * 0.7f, depth + 1);
                    }
                    p += dir * g * 1.3f;
                    dir = (dir + Random3(rnd) * 0.12f + Vector3.up * 0.03f).normalized;
                }
            }
            for (int i = 0; i < mainBranches; i++)
            {
                float a = i * Mathf.PI * 2f / mainBranches + (float)rnd.NextDouble() * 0.6f;
                var dir = new Vector3(Mathf.Cos(a), 0.25f + (float)rnd.NextDouble() * 0.5f, Mathf.Sin(a)).normalized;
                Branch(new Vector3(0, size * 0.06f, 0), dir, size * (0.35f + (float)rnd.NextDouble() * 0.2f), size * 0.07f, 0);
            }
            return b.ToMesh("Dendrita");
        }

        /// <summary>Barra cristalina (proceso de yoduro: titanio, circonio, hafnio) o barra lisa.</summary>
        public static Mesh Rod(int seed, float length, float radius, bool crystalline)
        {
            if (!crystalline)
            {
                // Barra fundida: cilindro con extremos redondeados y algo irregular
                var profile = new List<Vector2>();
                int n = 12;
                for (int i = 0; i <= n; i++)
                {
                    float a = i / (float)n * Mathf.PI * 0.5f;
                    profile.Add(new Vector2(Mathf.Sin(a) * radius, -length / 2 - radius * 0.4f + (1 - Mathf.Cos(a)) * radius * 0.4f));
                }
                for (int i = 0; i <= n; i++)
                {
                    float a = i / (float)n * Mathf.PI * 0.5f;
                    profile.Add(new Vector2(Mathf.Cos(a) * radius, length / 2 + Mathf.Sin(a) * radius * 0.4f));
                }
                profile.Insert(0, new Vector2(0, -length / 2 - radius * 0.4f));
                profile.Add(new Vector2(0, length / 2 + radius * 0.4f));
                return Lathe(profile.ToArray(), 28, "Barra");
            }
            var b = new Builder();
            var rnd = new System.Random(seed);
            // Nucleo + muchos cristalitos que sobresalen
            var core = Lathe(new[] { new Vector2(0, -length / 2), new Vector2(radius * 0.8f, -length / 2), new Vector2(radius * 0.8f, length / 2), new Vector2(0, length / 2) }, 10, "nucleo");
            b.Append(core, Matrix4x4.identity);
            int count = Mathf.RoundToInt(length / radius * 9);
            for (int i = 0; i < count; i++)
            {
                float y = ((float)rnd.NextDouble() - 0.5f) * length;
                float a = (float)rnd.NextDouble() * Mathf.PI * 2f;
                var outward = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                var pos = outward * radius * 0.7f + Vector3.up * y;
                var size = radius * (0.35f + (float)rnd.NextDouble() * 0.45f);
                var rot = Quaternion.LookRotation(outward) * Quaternion.Euler((float)rnd.NextDouble() * 90, (float)rnd.NextDouble() * 90, (float)rnd.NextDouble() * 90);
                b.Append(Rock(seed + i * 7, Vector3.one * size, 0.2f, 0, true, 0.6f), Matrix4x4.TRS(pos, rot, Vector3.one));
            }
            return b.ToMesh("BarraCristalina");
        }

        /// <summary>Escama electrolitica: placa irregular y delgada con superficie rugosa (hierro, cobalto, manganeso).</summary>
        public static Mesh Flake(int seed, float size, float thickness)
        {
            var rnd = new System.Random(seed);
            int n = 9;
            var b = new Builder { OutFrom = Vector3.zero };
            var top = new Vector3[n];
            var bot = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                float a = i * Mathf.PI * 2f / n + ((float)rnd.NextDouble() - 0.5f) * 0.4f;
                float r = size * (0.6f + (float)rnd.NextDouble() * 0.4f);
                var p = new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r * 0.8f);
                float bump = ((float)rnd.NextDouble() - 0.5f) * thickness * 0.8f;
                top[i] = p + Vector3.up * (thickness / 2 + bump);
                bot[i] = p - Vector3.up * (thickness / 2);
            }
            var cTop = Vector3.up * (thickness / 2 + thickness * 0.6f);
            var cBot = -Vector3.up * thickness / 2;
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                // Superficie superior granulada: dos anillos con bultos
                var mi = (top[i] + cTop) * 0.5f + Vector3.up * ((float)rnd.NextDouble() * thickness * 0.5f);
                var mj = (top[j] + cTop) * 0.5f + Vector3.up * ((float)rnd.NextDouble() * thickness * 0.5f);
                b.FlatTri(top[i], mi, top[j]);
                b.FlatTri(top[j], mi, mj);
                b.FlatTri(mi, cTop, mj);
                b.FlatTri(bot[j], cBot, bot[i]);
                b.FlatQuad(bot[i], top[i], top[j], bot[j]);
            }
            return b.ToMesh("Escama");
        }

        /// <summary>Cristal de bismuto en "tolva": marcos cuadrados escalonados. Devuelve una malla por escalon.</summary>
        public static Mesh[] Hopper(int steps, float size)
        {
            var meshes = new Mesh[steps];
            for (int s = 0; s < steps; s++)
            {
                var b = new Builder();
                float outer = size * (1f - s * 0.16f);
                float wall = size * 0.09f;
                float y0 = s * wall * 0.9f;
                float h = wall * 1.1f;
                // Cuatro listones que forman un marco
                for (int k = 0; k < 4; k++)
                {
                    var rot = Quaternion.Euler(0, k * 90f, 0);
                    var c = rot * new Vector3(0, y0 + h / 2, outer / 2 - wall / 2);
                    var tr = Matrix4x4.TRS(c, rot, new Vector3(outer, h, wall));
                    b.Append(LabFactory.Primitive(PrimitiveType.Cube), tr);
                    // Escalon interior pequeno
                    var c2 = rot * new Vector3(0, y0 + h * 0.3f, outer / 2 - wall * 1.4f);
                    b.Append(LabFactory.Primitive(PrimitiveType.Cube), Matrix4x4.TRS(c2, rot, new Vector3(outer - wall * 2f, h * 0.6f, wall * 0.8f)));
                }
                meshes[s] = b.ToMesh("Escalon" + s);
            }
            return meshes;
        }

        /// <summary>Monton de polvo (cono irregular).</summary>
        public static Mesh Pile(int seed, float radius, float height)
        {
            var b = new Builder();
            int rings = 7, seg = 28;
            var idx = new int[rings + 1, seg];
            for (int r = 0; r <= rings; r++)
            {
                float t = r / (float)rings;
                for (int s = 0; s < seg; s++)
                {
                    float a = s * Mathf.PI * 2f / seg;
                    float rr = radius * t * (0.85f + 0.3f * Noise(new Vector3(Mathf.Cos(a) * 2, Mathf.Sin(a) * 2, t * 3), seed));
                    float y = height * Mathf.Pow(1f - t, 1.6f) * (0.9f + 0.2f * Noise(new Vector3(a, t * 4, 1), seed + 3));
                    var p = new Vector3(Mathf.Cos(a) * rr, y, Mathf.Sin(a) * rr);
                    idx[r, s] = b.Vert(p, Vector3.up, new Vector2(p.x, p.z));
                }
            }
            for (int r = 0; r < rings; r++)
                for (int s = 0; s < seg; s++)
                {
                    int s2 = (s + 1) % seg;
                    b.Tri(idx[r, s], idx[r, s2], idx[r + 1, s]);
                    b.Tri(idx[r, s2], idx[r + 1, s2], idx[r + 1, s]);
                }
            OrientOutward(b.V, b.T, new Vector3(0, -height * 2f, 0));
            var m = b.ToMesh("Polvo");
            m.RecalculateNormals();
            return m;
        }

        /// <summary>Superficie de revolucion alrededor de Y a partir de un perfil (x = radio, y = altura).</summary>
        public static Mesh Lathe(Vector2[] profile, int segments, string name = "Torno")
        {
            var b = new Builder();
            int n = profile.Length;
            for (int s = 0; s <= segments; s++)
            {
                float a = s * Mathf.PI * 2f / segments;
                float c = Mathf.Cos(a), si = Mathf.Sin(a);
                for (int i = 0; i < n; i++)
                {
                    var p = profile[i];
                    // Normal del perfil: perpendicular al segmento
                    var prev = profile[Mathf.Max(0, i - 1)];
                    var next = profile[Mathf.Min(n - 1, i + 1)];
                    var tan = (next - prev).normalized;
                    var n2 = new Vector2(tan.y, -tan.x);
                    b.Vert(new Vector3(c * p.x, p.y, si * p.x), new Vector3(c * n2.x, n2.y, si * n2.x).normalized, new Vector2(s / (float)segments, i / (float)(n - 1)));
                }
            }
            for (int s = 0; s < segments; s++)
                for (int i = 0; i < n - 1; i++)
                {
                    int a = s * n + i, bb = (s + 1) * n + i;
                    b.Tri(a, a + 1, bb);
                    b.Tri(bb, a + 1, bb + 1);
                }
            var m = b.ToMesh(name);
            // Si el perfil va hacia abajo, las caras quedan al reves: se corrige comparando con el eje.
            var vs = m.vertices; var ns = m.normals; var ts = m.triangles;
            // Suma ponderada por area y radio: la pared exterior (mas grande) decide la orientacion.
            float sum = 0f;
            for (int i = 0; i < ts.Length; i += 3)
            {
                var c = (vs[ts[i]] + vs[ts[i + 1]] + vs[ts[i + 2]]) / 3f;
                var fn = Vector3.Cross(vs[ts[i + 1]] - vs[ts[i]], vs[ts[i + 2]] - vs[ts[i]]);
                sum += Vector3.Dot(fn, new Vector3(c.x, 0, c.z));
            }
            if (sum < 0f)
            {
                for (int i = 0; i < ts.Length; i += 3) { int t = ts[i + 1]; ts[i + 1] = ts[i + 2]; ts[i + 2] = t; }
                m.triangles = ts;
            }
            m.RecalculateNormals();
            return m;
        }

        /// <summary>Ampolla de vidrio sellada (cilindro con fondo redondo y punta estirada).</summary>
        public static Mesh Ampoule(float radius, float length)
        {
            var p = new List<Vector2>();
            int n = 8;
            for (int i = 0; i <= n; i++)
            {
                float a = i / (float)n * Mathf.PI * 0.5f;
                p.Add(new Vector2(Mathf.Sin(a) * radius, radius - Mathf.Cos(a) * radius));
            }
            p.Add(new Vector2(radius, length * 0.72f));
            p.Add(new Vector2(radius * 0.92f, length * 0.78f));
            p.Add(new Vector2(radius * 0.45f, length * 0.86f));
            p.Add(new Vector2(radius * 0.18f, length * 0.92f));
            p.Add(new Vector2(radius * 0.12f, length));
            p.Add(new Vector2(0, length + radius * 0.06f));
            p.Insert(0, new Vector2(0, 0));
            return Lathe(p.ToArray(), 24, "Ampolla");
        }

        /// <summary>Capsula (cilindro con extremos redondos) a lo largo de Y, de 0 a "length": tubos de descarga.</summary>
        public static Mesh Capsule(float length, float radius)
        {
            var p = new List<Vector2> { new Vector2(0, 0) };
            for (int i = 1; i <= 8; i++)
            {
                float a = i / 8f * Mathf.PI * 0.5f;
                p.Add(new Vector2(Mathf.Sin(a) * radius, radius - Mathf.Cos(a) * radius));
            }
            for (int i = 0; i <= 8; i++)
            {
                float a = i / 8f * Mathf.PI * 0.5f;
                p.Add(new Vector2(Mathf.Cos(a) * radius, length - radius + Mathf.Sin(a) * radius));
            }
            p.Add(new Vector2(0, length));
            return Lathe(p.ToArray(), 18, "Capsula");
        }
        /// <summary>Plato de vidrio de reloj (para polvos y gotas).</summary>
        public static Mesh WatchGlass(float radius, float depth)
        {
            var p = new List<Vector2>();
            for (int i = 0; i <= 10; i++)
            {
                float t = i / 10f;
                p.Add(new Vector2(t * radius, depth * t * t));
            }
            return Lathe(p.ToArray(), 32, "VidrioReloj");
        }

        /// <summary>Charco o gota aplanada de liquido (mercurio).</summary>
        public static Mesh Puddle(int seed, float radius, float height)
        {
            var m = Rock(seed, new Vector3(radius, height, radius * 0.8f), 0.12f, 3, false);
            var vs = m.vertices;
            for (int i = 0; i < vs.Length; i++) if (vs[i].y < 0) vs[i].y *= 0.15f;
            m.vertices = vs;
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        /// <summary>
        /// Esfera vista desde adentro con coordenadas para una foto 360 (equirectangular).
        /// El centro de la foto (u = 0.5) queda hacia -Z.
        /// </summary>
        public static Mesh PanoSphere(int segU = 48, int segV = 24)
        {
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>(); var col = new List<Color32>();
            for (int j = 0; j <= segV; j++)
            {
                float lat = Mathf.PI * (j / (float)segV - 0.5f); // -90 .. 90
                for (int i = 0; i <= segU; i++)
                {
                    float u = i / (float)segU;
                    float lon = (u - 0.5f) * Mathf.PI * 2f;
                    // u = 0.5 -> -Z; al crecer u se gira hacia la derecha visto desde adentro
                    var p = new Vector3(-Mathf.Sin(lon) * Mathf.Cos(lat), Mathf.Sin(lat), -Mathf.Cos(lon) * Mathf.Cos(lat));
                    v.Add(p);
                    uv.Add(new Vector2(u, j / (float)segV));
                    col.Add(new Color32(255, 255, 255, 255));
                }
            }
            int row = segU + 1;
            for (int j = 0; j < segV; j++)
                for (int i = 0; i < segU; i++)
                {
                    int a = j * row + i, b = a + 1, c = a + row, d = c + 1;
                    // Orden para que las caras miren hacia adentro
                    t.Add(a); t.Add(c); t.Add(b);
                    t.Add(b); t.Add(c); t.Add(d);
                }
            var m = new Mesh { name = "Panorama" };
            m.SetVertices(v); m.SetUVs(0, uv); m.SetColors(col); m.SetTriangles(t, 0);
            var n = new Vector3[v.Count];
            for (int i = 0; i < n.Length; i++) n[i] = -v[i];
            m.normals = n;
            m.RecalculateBounds();
            return m;
        }

        /// <summary>Esfera lisa de baja resolucion (bolitas, globos de vidrio).</summary>
        public static Mesh Sphere(int subdiv = 2)
        {
            Icosphere(subdiv, out var vs, out var ts);
            return Finish(vs, ts, false, "Esfera");
        }
    }
}

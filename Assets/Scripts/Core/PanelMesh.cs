using System.Collections.Generic;
using UnityEngine;

namespace LabQuimicaVR
{
    /// <summary>
    /// Rectangulos redondeados con grosor para pantallas, tarjetas y botones. El color va en los vertices
    /// (degradado de arriba hacia abajo) y la UV es la posicion en metros, para que el patron del material
    /// tenga el mismo tamano en todos los paneles. La cara frontal mira hacia -Z, como los textos.
    /// </summary>
    public static class PanelMesh
    {
        static readonly Dictionary<string, Mesh> cache = new Dictionary<string, Mesh>();

        public static Mesh Rounded(Vector2 size, float radius, float depth, Color top, Color bottom, int segments = 6)
        {
            radius = Mathf.Clamp(radius, 0.001f, Mathf.Min(size.x, size.y) * 0.5f);
            string key = $"{size.x:0.###}_{size.y:0.###}_{radius:0.###}_{depth:0.###}_{ColorUtility.ToHtmlStringRGB(top)}_{ColorUtility.ToHtmlStringRGB(bottom)}";
            if (cache.TryGetValue(key, out var cached) && cached != null) return cached;

            // Contorno en sentido antihorario empezando por la esquina superior derecha.
            var ring = new List<Vector2>();
            var corners = new[]
            {
                new Vector2(size.x / 2 - radius, size.y / 2 - radius), new Vector2(-size.x / 2 + radius, size.y / 2 - radius),
                new Vector2(-size.x / 2 + radius, -size.y / 2 + radius), new Vector2(size.x / 2 - radius, -size.y / 2 + radius),
            };
            for (int c = 0; c < 4; c++)
                for (int i = 0; i <= segments; i++)
                {
                    float ang = (c * 90f + i * 90f / segments) * Mathf.Deg2Rad;
                    ring.Add(corners[c] + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * radius);
                }

            var v = new List<Vector3>();
            var col = new List<Color>();
            var uv = new List<Vector2>();
            var tri = new List<int>();
            float zf = -depth / 2f, zb = depth / 2f;
            Color At(float y) => Color.Lerp(bottom, top, Mathf.InverseLerp(-size.y / 2, size.y / 2, y));
            var side = Color.Lerp(bottom, Color.black, 0.35f);

            void Tri(int a, int b, int c, Vector3 normal)
            {
                // Unity dibuja la cara cuyo producto cruz apunta hacia quien la mira.
                if (Vector3.Dot(Vector3.Cross(v[b] - v[a], v[c] - v[a]), normal) < 0) { tri.Add(a); tri.Add(c); tri.Add(b); }
                else { tri.Add(a); tri.Add(b); tri.Add(c); }
            }

            // Cara frontal y trasera (abanico desde el centro: el degradado lineal se conserva exacto).
            foreach (var (z, n, front) in new[] { (zf, Vector3.back, true), (zb, Vector3.forward, false) })
            {
                int center = v.Count;
                v.Add(new Vector3(0, 0, z)); col.Add(front ? At(0) : side); uv.Add(Vector2.zero);
                foreach (var p in ring) { v.Add(new Vector3(p.x, p.y, z)); col.Add(front ? At(p.y) : side); uv.Add(p); }
                for (int i = 0; i < ring.Count; i++)
                    Tri(center, center + 1 + i, center + 1 + (i + 1) % ring.Count, n);
            }

            // Cantos
            for (int i = 0; i < ring.Count; i++)
            {
                var p0 = ring[i];
                var p1 = ring[(i + 1) % ring.Count];
                var mid = (p0 + p1) * 0.5f;
                var dir = p1 - p0;
                var normal = new Vector3(dir.y, -dir.x, 0).normalized;
                if (Vector2.Dot(new Vector2(normal.x, normal.y), mid) < 0) normal = -normal;
                int s = v.Count;
                v.Add(new Vector3(p0.x, p0.y, zf)); v.Add(new Vector3(p1.x, p1.y, zf));
                v.Add(new Vector3(p1.x, p1.y, zb)); v.Add(new Vector3(p0.x, p0.y, zb));
                for (int k = 0; k < 4; k++) { col.Add(side); uv.Add(p0); }
                Tri(s, s + 1, s + 2, normal);
                Tri(s, s + 2, s + 3, normal);
            }

            var mesh = new Mesh { name = "Panel_" + key };
            mesh.SetVertices(v);
            mesh.SetColors(col);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(tri, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            cache[key] = mesh;
            return mesh;
        }
    }
}

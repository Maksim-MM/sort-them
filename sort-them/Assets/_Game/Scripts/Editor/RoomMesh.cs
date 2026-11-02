using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SortThem.Editor
{
    public static class RoomMesh
    {
        public const string Folder = Paths.Meshes + "/Room";
        public const float PackMargin = 0.015f;

        static readonly Dictionary<string, Mesh> Cache = new Dictionary<string, Mesh>();

        public static void ClearCache() => Cache.Clear();

        public static GameObject Box(string name, Transform parent, Vector3 center, Vector3 size, Material material, float metersPerTile = 1f, bool grainAlongLongest = true)
            => Box(name, parent, center, size, material, new Vector2(metersPerTile, metersPerTile), grainAlongLongest);

        public static GameObject Box(string name, Transform parent, Vector3 center, Vector3 size, Material material, Vector2 metersPerTile, bool grainAlongLongest = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = Get(size, metersPerTile, grainAlongLongest);
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = material;
            go.isStatic = true;
            return go;
        }

        public static Mesh Get(Vector3 size, Vector2 metersPerTile, bool grainAlongLongest = true)
        {
            string key = size.x.ToString("0.###") + "_" + size.y.ToString("0.###") + "_" + size.z.ToString("0.###") + "_" + metersPerTile.x.ToString("0.###") + "x" + metersPerTile.y.ToString("0.###") + (grainAlongLongest ? "" : "_flat");
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

            string path = Folder + "/Box_" + key.Replace('.', 'p') + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null)
            {
                Cache[key] = existing;
                return existing;
            }

            var mesh = Build(size, metersPerTile, grainAlongLongest);
            EditorAssets.EnsureFolder(Folder);
            AssetDatabase.CreateAsset(mesh, path);
            Cache[key] = mesh;
            return mesh;
        }

        public const float Bevel = 0.008f;
        public const int BevelSegments = 2;
        public const float BevelMaxFraction = 0.35f;

        static Mesh Build(Vector3 size, Vector2 metersPerTile, bool grainAlongLongest) => Build(size, metersPerTile, grainAlongLongest, Bevel, BevelSegments);

        public static Mesh Build(Vector3 size, Vector2 metersPerTile, bool grainAlongLongest, float bevel, int segments)
        {
            var h = size * 0.5f;
            float r = Mathf.Min(bevel, Mathf.Min(size.x, Mathf.Min(size.y, size.z)) * BevelMaxFraction);
            if (r <= 0f) segments = 0;
            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var uv0 = new List<Vector2>();
            var tris = new List<int>();
            FaceCoords.Clear();
            FaceCount = 0;

            AddFace(verts, norms, uv0, tris, h, r, segments, new Vector3(0, 0, -h.z), Vector3.right * h.x, Vector3.up * h.y, Vector3.back, size.x, size.y, metersPerTile, grainAlongLongest);
            AddFace(verts, norms, uv0, tris, h, r, segments, new Vector3(0, 0, h.z), Vector3.left * h.x, Vector3.up * h.y, Vector3.forward, size.x, size.y, metersPerTile, grainAlongLongest);
            AddFace(verts, norms, uv0, tris, h, r, segments, new Vector3(-h.x, 0, 0), Vector3.back * h.z, Vector3.up * h.y, Vector3.left, size.z, size.y, metersPerTile, grainAlongLongest);
            AddFace(verts, norms, uv0, tris, h, r, segments, new Vector3(h.x, 0, 0), Vector3.forward * h.z, Vector3.up * h.y, Vector3.right, size.z, size.y, metersPerTile, grainAlongLongest);
            AddFace(verts, norms, uv0, tris, h, r, segments, new Vector3(0, h.y, 0), Vector3.right * h.x, Vector3.forward * h.z, Vector3.up, size.x, size.z, metersPerTile, grainAlongLongest);
            AddFace(verts, norms, uv0, tris, h, r, segments, new Vector3(0, -h.y, 0), Vector3.right * h.x, Vector3.back * h.z, Vector3.down, size.x, size.z, metersPerTile, grainAlongLongest);

            var mesh = new Mesh { name = "Box" };
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uv0);
            mesh.SetUVs(1, PackFaceCharts(size, FaceCoords));
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            FaceCoords.Clear();
            return mesh;
        }

        public const float ChartMargin = 0.03f;
        static readonly List<(int face, float fa, float fb)> FaceCoords = new List<(int, float, float)>();
        static int FaceCount;

        static List<Vector2> PackFaceCharts(Vector3 size, List<(int face, float fa, float fb)> coords)
        {
            var dims = new[] { new Vector2(size.x, size.y), new Vector2(size.x, size.y), new Vector2(size.z, size.y), new Vector2(size.z, size.y), new Vector2(size.x, size.z), new Vector2(size.x, size.z) };
            var rot = new bool[6];
            var rect = new Vector2[6];
            for (int i = 0; i < 6; i++) { rot[i] = dims[i].y > dims[i].x; rect[i] = rot[i] ? new Vector2(dims[i].y, dims[i].x) : dims[i]; }
            var order = new List<int> { 0, 1, 2, 3, 4, 5 };
            order.Sort((a, b) => rect[b].y.CompareTo(rect[a].y));
            float lo = 0f, hi = 1f / Mathf.Max(0.0001f, Mathf.Max(rect[0].x, Mathf.Max(rect[2].x, rect[4].x)));
            var origin = new Vector2[6];
            bool TryPack(float s, bool store)
            {
                float m = ChartMargin, x = m, y = m, rowH = 0f;
                foreach (int i in order)
                {
                    float w = rect[i].x * s, h = rect[i].y * s;
                    if (x + w > 1f - m + 1e-6f) { x = m; y += rowH + m; rowH = 0f; }
                    if (x + w > 1f - m + 1e-6f || y + h > 1f - m + 1e-6f) return false;
                    if (store) origin[i] = new Vector2(x, y);
                    x += w + m; rowH = Mathf.Max(rowH, h);
                }
                return true;
            }
            for (int it = 0; it < 40; it++) { float mid = (lo + hi) * 0.5f; if (TryPack(mid, false)) lo = mid; else hi = mid; }
            TryPack(lo, true);
            var uv2 = new List<Vector2>(coords.Count);
            foreach (var (face, fa, fb) in coords)
            {
                float u = rot[face] ? fb : fa, v = rot[face] ? fa : fb;
                uv2.Add(origin[face] + new Vector2(u * rect[face].x, v * rect[face].y) * lo);
            }
            return uv2;
        }

        [MenuItem("SortThem/Console/Rebuild Room Boxes (bevel)")]
        public static void RebuildAll()
        {
            if (EditorApplication.isPlaying) { Debug.LogError("RoomMesh: останови Play."); return; }
            var guids = AssetDatabase.FindAssets("t:Mesh Box_", new[] { Folder });
            int done = 0, skipped = 0;
            var report = new System.Text.StringBuilder();
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string name = System.IO.Path.GetFileNameWithoutExtension(path);
                if (!TryParseKey(name, out var size, out var tile, out bool grain)) { skipped++; report.Append(name).Append(" skip; "); continue; }
                var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (mesh == null) { skipped++; continue; }
                var fresh = Build(size, tile, grain, Bevel, BevelSegments);
                mesh.Clear();
                mesh.SetVertices(fresh.vertices);
                mesh.SetNormals(fresh.normals);
                mesh.SetUVs(0, fresh.uv);
                mesh.SetUVs(1, fresh.uv2);
                mesh.SetTangents(fresh.tangents);
                mesh.SetTriangles(fresh.triangles, 0);
                mesh.RecalculateBounds();
                mesh.UploadMeshData(false);
                Object.DestroyImmediate(fresh);
                EditorUtility.SetDirty(mesh);
                done++;
            }
            AssetDatabase.SaveAssets();
            ClearCache();
            Debug.Log("RoomMesh: rebuilt " + done + " box meshes with bevel " + Bevel + " m (" + BevelSegments + " segments), skipped " + skipped + ". " + report);
        }

        static bool TryParseKey(string name, out Vector3 size, out Vector2 tile, out bool grain)
        {
            size = Vector3.zero; tile = Vector2.one; grain = true;
            if (!name.StartsWith("Box_")) return false;
            string key = name.Substring(4);
            if (key.EndsWith("_flat")) { grain = false; key = key.Substring(0, key.Length - 5); }
            var parts = key.Split('_');
            if (parts.Length != 4) return false;
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            float P(string v) => float.Parse(v.Replace('p', '.'), inv);
            try
            {
                size = new Vector3(P(parts[0]), P(parts[1]), P(parts[2]));
                var t = parts[3].Split('x');
                if (t.Length != 2) return false;
                tile = new Vector2(P(t[0]), P(t[1]));
            }
            catch { return false; }
            return size.x > 0f && size.y > 0f && size.z > 0f;
        }

        public static void GenerateLightmapUVs(Mesh mesh)
        {
            var param = new UnwrapParam();
            UnwrapParam.SetDefaults(out param);
            param.hardAngle = 88f;
            param.angleError = 0.08f;
            param.areaError = 0.15f;
            param.packMargin = PackMargin;
            Unwrapping.GenerateSecondaryUVSet(mesh, param);
        }

        static void AddFace(List<Vector3> verts, List<Vector3> norms, List<Vector2> uv0, List<int> tris, Vector3 half, float r, int segments, Vector3 center, Vector3 right, Vector3 up, Vector3 normal, float width, float height, Vector2 metersPerTile, bool grainAlongLongest)
        {
            int faceIndex = FaceCount++;
            float hr = right.magnitude, hu = up.magnitude;
            var ca = Coords(hr, r, segments);
            var cb = Coords(hu, r, segments);
            var inner = half - Vector3.one * r;
            float tu = width / metersPerTile.x, tv = height / metersPerTile.y;
            bool swap = grainAlongLongest && height > width;
            int start = verts.Count;
            int na = ca.Length, nb = cb.Length;
            for (int j = 0; j < nb; j++)
            {
                for (int i = 0; i < na; i++)
                {
                    var p = center + right * ca[i] + up * cb[j];
                    var q = new Vector3(Mathf.Clamp(p.x, -inner.x, inner.x), Mathf.Clamp(p.y, -inner.y, inner.y), Mathf.Clamp(p.z, -inner.z, inner.z));
                    var d = p - q;
                    Vector3 n = d.sqrMagnitude > 1e-12f ? d.normalized : normal;
                    verts.Add(r > 0f ? q + n * r : p);
                    norms.Add(n);
                    float fa = (ca[i] + 1f) * 0.5f, fb = (cb[j] + 1f) * 0.5f;
                    uv0.Add(swap ? new Vector2(fb * tv, fa * tu) : new Vector2(fa * tu, fb * tv));
                    FaceCoords.Add((faceIndex, fa, fb));
                }
            }
            for (int j = 0; j < nb - 1; j++)
            {
                for (int i = 0; i < na - 1; i++)
                {
                    int v00 = start + j * na + i, v10 = v00 + 1, v01 = v00 + na, v11 = v01 + 1;
                    tris.Add(v00); tris.Add(v11); tris.Add(v10);
                    tris.Add(v00); tris.Add(v01); tris.Add(v11);
                }
            }
        }

        static float[] Coords(float halfExtent, float r, int segments)
        {
            if (segments <= 0 || r <= 0f || halfExtent <= r) return new[] { -1f, 1f };
            var list = new List<float>();
            for (int k = 0; k <= segments; k++)
            {
                float offset = r * (1f - Mathf.Cos(Mathf.PI * 0.5f * k / segments));
                list.Add(-1f + offset / halfExtent);
            }
            for (int k = segments; k >= 0; k--)
            {
                float offset = r * (1f - Mathf.Cos(Mathf.PI * 0.5f * k / segments));
                list.Add(1f - offset / halfExtent);
            }
            return list.ToArray();
        }
    }
}

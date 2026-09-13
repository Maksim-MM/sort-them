using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SortThem.Editor
{
    public static class RoomMesh
    {
        public const string Folder = Paths.Meshes + "/Room";

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

        static Mesh Build(Vector3 size, Vector2 metersPerTile, bool grainAlongLongest)
        {
            var h = size * 0.5f;
            var verts = new List<Vector3>(24);
            var norms = new List<Vector3>(24);
            var uv0 = new List<Vector2>(24);
            var uv1 = new List<Vector2>(24);
            var tris = new List<int>(36);

            AddFace(verts, norms, uv0, tris, new Vector3(0, 0, -h.z), Vector3.right * h.x, Vector3.up * h.y, Vector3.back, size.x, size.y, metersPerTile, grainAlongLongest);
            AddFace(verts, norms, uv0, tris, new Vector3(0, 0, h.z), Vector3.left * h.x, Vector3.up * h.y, Vector3.forward, size.x, size.y, metersPerTile, grainAlongLongest);
            AddFace(verts, norms, uv0, tris, new Vector3(-h.x, 0, 0), Vector3.back * h.z, Vector3.up * h.y, Vector3.left, size.z, size.y, metersPerTile, grainAlongLongest);
            AddFace(verts, norms, uv0, tris, new Vector3(h.x, 0, 0), Vector3.forward * h.z, Vector3.up * h.y, Vector3.right, size.z, size.y, metersPerTile, grainAlongLongest);
            AddFace(verts, norms, uv0, tris, new Vector3(0, h.y, 0), Vector3.right * h.x, Vector3.forward * h.z, Vector3.up, size.x, size.z, metersPerTile, grainAlongLongest);
            AddFace(verts, norms, uv0, tris, new Vector3(0, -h.y, 0), Vector3.right * h.x, Vector3.back * h.z, Vector3.down, size.x, size.z, metersPerTile, grainAlongLongest);

            const float pad = 0.02f;
            for (int face = 0; face < 6; face++)
            {
                float cx = (face % 3) / 3f, cy = (face / 3) * 0.5f;
                uv1.Add(new Vector2(cx + pad / 3f, cy + pad * 0.5f));
                uv1.Add(new Vector2(cx + (1f - pad) / 3f, cy + pad * 0.5f));
                uv1.Add(new Vector2(cx + (1f - pad) / 3f, cy + (1f - pad) * 0.5f));
                uv1.Add(new Vector2(cx + pad / 3f, cy + (1f - pad) * 0.5f));
            }

            var mesh = new Mesh { name = "Box" };
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uv0);
            mesh.SetUVs(1, uv1);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        static void AddFace(List<Vector3> verts, List<Vector3> norms, List<Vector2> uv0, List<int> tris, Vector3 center, Vector3 right, Vector3 up, Vector3 normal, float width, float height, Vector2 metersPerTile, bool grainAlongLongest)
        {
            int start = verts.Count;
            verts.Add(center - right - up);
            verts.Add(center + right - up);
            verts.Add(center + right + up);
            verts.Add(center - right + up);
            for (int i = 0; i < 4; i++) norms.Add(normal);

            float tu = width / metersPerTile.x, tv = height / metersPerTile.y;
            if (grainAlongLongest && height > width)
            {
                uv0.Add(new Vector2(0f, 0f));
                uv0.Add(new Vector2(0f, tu));
                uv0.Add(new Vector2(tv, tu));
                uv0.Add(new Vector2(tv, 0f));
            }
            else
            {
                uv0.Add(new Vector2(0f, 0f));
                uv0.Add(new Vector2(tu, 0f));
                uv0.Add(new Vector2(tu, tv));
                uv0.Add(new Vector2(0f, tv));
            }

            tris.Add(start); tris.Add(start + 2); tris.Add(start + 1);
            tris.Add(start); tris.Add(start + 3); tris.Add(start + 2);
        }
    }
}

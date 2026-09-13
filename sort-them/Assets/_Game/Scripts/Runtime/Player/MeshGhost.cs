using System.Collections.Generic;
using UnityEngine;

namespace SortThem
{
    public class MeshGhost : MonoBehaviour
    {
        public MeshFilter Filter;
        public MeshRenderer Renderer;

        const int CacheLimit = 32;
        static readonly Dictionary<Mesh, Mesh> Cache = new Dictionary<Mesh, Mesh>();
        static readonly List<Mesh> Order = new List<Mesh>();

        public void Show(Mesh mesh, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            var smooth = Smoothed(mesh);
            if (Filter.sharedMesh != smooth) Filter.sharedMesh = smooth;
            transform.SetPositionAndRotation(position, rotation);
            transform.localScale = scale;
            if (!gameObject.activeSelf) gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (gameObject.activeSelf) gameObject.SetActive(false);
        }

        public static Mesh Smoothed(Mesh src)
        {
            if (src == null || !src.isReadable) return src;
            if (Cache.TryGetValue(src, out var cached) && cached != null) return cached;
            var verts = src.vertices;
            var normals = src.normals;
            if (normals == null || normals.Length != verts.Length) return src;
            var sums = new Dictionary<Vector3Int, Vector3>(verts.Length);
            var keys = new Vector3Int[verts.Length];
            for (int i = 0; i < verts.Length; i++)
            {
                var k = new Vector3Int(Mathf.RoundToInt(verts[i].x * 10000f), Mathf.RoundToInt(verts[i].y * 10000f), Mathf.RoundToInt(verts[i].z * 10000f));
                keys[i] = k;
                sums.TryGetValue(k, out var s);
                sums[k] = s + normals[i];
            }
            var outNormals = new Vector3[verts.Length];
            for (int i = 0; i < verts.Length; i++)
            {
                var s = sums[keys[i]];
                outNormals[i] = s.sqrMagnitude > 1e-12f ? s.normalized : normals[i];
            }
            var mesh = new Mesh { name = src.name + "_outline" };
            mesh.indexFormat = src.indexFormat;
            mesh.vertices = verts;
            mesh.normals = outNormals;
            mesh.subMeshCount = src.subMeshCount;
            for (int s = 0; s < src.subMeshCount; s++) mesh.SetIndices(src.GetIndices(s), src.GetTopology(s), s);
            mesh.bounds = src.bounds;
            mesh.UploadMeshData(true);
            if (Order.Count >= CacheLimit)
            {
                var old = Order[0];
                Order.RemoveAt(0);
                if (Cache.TryGetValue(old, out var oldMesh)) { Cache.Remove(old); if (oldMesh != null) Destroy(oldMesh); }
            }
            Cache[src] = mesh;
            Order.Add(src);
            return mesh;
        }
    }
}

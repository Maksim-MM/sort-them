using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SortThem
{
    public static class SpecialCarAssembly
    {
        public static Transform Build(SpecialCarData data, int level, Transform parent, int layer, List<GameObject> newParts = null)
        {
            var root = new GameObject("Assembly").transform;
            root.SetParent(parent, false);
            root.localPosition = data.AssemblyOffset;
            root.localRotation = Quaternion.identity;
            root.localScale = Vector3.one * data.AssemblyScale;
            root.gameObject.layer = layer;

            int max = Mathf.Clamp(level, 0, data.MaxLevel);
            var removed = new HashSet<GameObject>();
            for (int i = 0; i < max; i++)
                foreach (var r in data.Steps[i].Remove) if (r != null) removed.Add(r);
            foreach (var p in data.BaseParts)
                if (p.Prefab != null && !removed.Contains(p.Prefab)) Spawn(p, root, layer);
            for (int i = 0; i < max; i++)
                foreach (var p in data.Steps[i].Add)
                {
                    var go = Spawn(p, root, layer);
                    if (go != null && newParts != null && i == max - 1) newParts.Add(go);
                }
            return root;
        }

        public static Bounds LocalBounds(Transform root)
        {
            var b = new Bounds(Vector3.zero, Vector3.zero);
            bool first = true;
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.sharedMesh == null) continue;
                var mb = mf.sharedMesh.bounds;
                var m = root.worldToLocalMatrix * mf.transform.localToWorldMatrix;
                for (int i = 0; i < 8; i++)
                {
                    var c = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                    var p = m.MultiplyPoint3x4(c);
                    if (first) { b = new Bounds(p, Vector3.zero); first = false; }
                    else b.Encapsulate(p);
                }
            }
            return b;
        }

        static GameObject Spawn(SpecialCarData.Part part, Transform root, int layer)
        {
            if (part.Prefab == null) return null;
            var mf = part.Prefab.GetComponent<MeshFilter>();
            var mr = part.Prefab.GetComponent<MeshRenderer>();
            if (mf == null || mr == null || mf.sharedMesh == null) return null;
            var go = new GameObject(part.Prefab.name);
            go.layer = layer;
            go.transform.SetParent(root, false);
            go.transform.localPosition = part.LocalPosition;
            go.transform.localRotation = Quaternion.Euler(part.LocalEuler);
            go.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterials = mr.sharedMaterials;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.lightProbeUsage = LightProbeUsage.BlendProbes;
            r.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return go;
        }
    }
}

using UnityEditor;
using UnityEngine;

namespace SortThem.Editor
{
    public static class BombSetup
    {
        [MenuItem("SortThem/3f. Generate Bomb")]
        public static void Generate()
        {
            EditorAssets.EnsureFolder(Paths.Prefabs);
            var bodyMat = EditorAssets.Lit("BombBody", new Color(0.08f, 0.08f, 0.09f));
            var fuseMat = EditorAssets.Lit("BombFuse", new Color(0.76f, 0.65f, 0.45f));
            var sparkMat = EditorAssets.Unlit("BombSpark", new Color(1f, 0.55f, 0.1f));

            var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            body.name = "Bomb";
            body.layer = LayerMask.NameToLayer("LooseItems");
            body.transform.localScale = Vector3.one * 0.08f;
            body.GetComponent<Renderer>().sharedMaterial = bodyMat;
            var rb = body.AddComponent<Rigidbody>();
            rb.mass = 0.4f;
            rb.angularDamping = 0.5f;

            var fuseRoot = new GameObject("FuseRoot").transform;
            fuseRoot.SetParent(body.transform, false);
            fuseRoot.localPosition = new Vector3(0f, 0.48f, 0f);
            fuseRoot.localRotation = Quaternion.Euler(0f, 0f, -25f);

            var fuse = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            fuse.name = "Fuse";
            fuse.transform.SetParent(fuseRoot, false);
            fuse.transform.localPosition = new Vector3(0f, 0.375f, 0f);
            fuse.transform.localScale = new Vector3(0.075f, 0.375f, 0.075f);
            fuse.GetComponent<Renderer>().sharedMaterial = fuseMat;
            Object.DestroyImmediate(fuse.GetComponent<Collider>());

            var tip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            tip.name = "Tip";
            tip.transform.SetParent(fuseRoot, false);
            tip.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            tip.transform.localScale = Vector3.one * 0.25f;
            tip.GetComponent<Renderer>().sharedMaterial = sparkMat;
            Object.DestroyImmediate(tip.GetComponent<Collider>());

            var hiss = body.AddComponent<AudioSource>();
            hiss.playOnAwake = false;
            hiss.loop = true;
            hiss.spatialBlend = 1f;
            hiss.minDistance = 1f;
            hiss.maxDistance = 15f;
            hiss.rolloffMode = AudioRolloffMode.Linear;

            var bomb = body.AddComponent<Bomb>();
            bomb.Fuse = fuse.transform;
            bomb.Tip = tip.transform;
            bomb.Hiss = hiss;

            string prefabPath = Paths.Prefabs + "/Bomb.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(body, prefabPath);
            Object.DestroyImmediate(body);

            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(Paths.Config + "/GameConfig.asset");
            if (config != null)
            {
                config.BombPrefab = prefab;
                EditorUtility.SetDirty(config);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("SortThem: bomb prefab generated at " + prefabPath);
        }
    }
}

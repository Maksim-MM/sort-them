using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SortThem.Editor
{
    public static class ArcadeCarImporter
    {
        const string PoolRoot = "Assets/_Game/Art/CarPool";
        const string ArcadeMaterials = Paths.Materials + "/Arcade";
        const string ArcadeMeshes = Paths.Meshes + "/Arcade";
        const float TargetScale = 0.075f, MaxLength = 0.42f, MaxHeight = 0.30f;

        static readonly (string Folder, string Id, string Ru, string En, Color Color)[] Categories =
        {
            ("01_Sedans", "sedans", "Седаны", "Sedans", new Color(0.35f, 0.55f, 0.85f)),
            ("02_Hatch_Micro", "hatchbacks", "Хэтчбеки", "Hatchbacks", new Color(0.95f, 0.75f, 0.25f)),
            ("03_SUV_Offroad", "suv", "Внедорожники", "SUVs", new Color(0.45f, 0.6f, 0.35f)),
            ("04_Pickups", "pickups", "Пикапы", "Pickups", new Color(0.8f, 0.5f, 0.25f)),
            ("05_Muscle", "muscle", "Маслкары", "Muscle Cars", new Color(0.8f, 0.25f, 0.25f)),
            ("06_Supercars", "supercars", "Суперкары", "Supercars", new Color(0.9f, 0.85f, 0.2f)),
            ("07_Sport_Coupe", "sportcoupe", "Спорткупе", "Sport Coupes", new Color(0.9f, 0.4f, 0.55f)),
            ("08_Classic_Sport", "classicsport", "Классические спорткары", "Classic Sports", new Color(0.6f, 0.45f, 0.75f)),
            ("09_GT_ClassA", "gta", "Гонки GT A", "GT Racing A", new Color(0.2f, 0.7f, 0.85f)),
            ("10_GT_ClassR", "gtr", "Гонки GT R", "GT Racing R", new Color(0.25f, 0.45f, 0.75f)),
            ("11_Rally_Tuning", "rally", "Ралли и тюнинг", "Rally & Tuning", new Color(0.95f, 0.55f, 0.15f)),
            ("12_Extreme", "extreme", "Экстрим", "Extreme", new Color(0.55f, 0.85f, 0.3f)),
            ("13_Police", "police", "Полиция", "Police", new Color(0.2f, 0.3f, 0.6f)),
            ("14_Emergency_Service", "service", "Спецслужбы", "Emergency & Service", new Color(0.9f, 0.3f, 0.2f)),
            ("15_Vans_FoodTrucks", "vans", "Фургоны и фудтраки", "Vans & Food Trucks", new Color(0.5f, 0.8f, 0.75f)),
        };

        [MenuItem("SortThem/3g. Import ARCADE Cars")]
        public static void Import()
        {
            EditorAssets.EnsureFolder(Paths.Categories);
            EditorAssets.EnsureFolder(Paths.Cars);
            EditorAssets.EnsureFolder(Paths.CarPrefabs);
            EditorAssets.EnsureFolder(Paths.Meshes);
            EditorAssets.EnsureFolder(ArcadeMaterials);
            EditorAssets.EnsureFolder(ArcadeMeshes);
            ClearFolder(Paths.Cars, "*.asset");
            ClearFolder(Paths.CarPrefabs, "*.prefab");
            ClearFolder(Paths.Categories, "*.asset");
            ClearFolder(Paths.Racks, "*.asset");
            ClearFolder(Paths.Meshes, "car_*.asset");
            ClearFolder(ArcadeMeshes, "*.asset");

            var catalog = EditorAssets.LoadOrCreate<CarCatalog>(Paths.Catalog);
            var categories = new List<CategoryData>();
            var cars = new List<CarItemData>();
            var materials = new Dictionary<Material, Material>();
            int total = 0, tris = 0;
            try
            {
                for (int c = 0; c < Categories.Length; c++)
                {
                    var def = Categories[c];
                    string catId = "cat_" + def.Id;
                    var cat = EditorAssets.LoadOrCreate<CategoryData>(Paths.Categories + "/" + catId + ".asset");
                    cat.CategoryID = catId;
                    cat.DevName = def.En;
                    cat.CategoryColor = def.Color;
                    LocUtil.Set("cat." + catId, def.Ru, def.En);
                    cat.DisplayName = LocUtil.Ref("cat." + catId);
                    EditorUtility.SetDirty(cat);
                    categories.Add(cat);

                    string folder = PoolRoot + "/" + def.Folder;
                    var prefabPaths = AssetDatabase.FindAssets("t:Prefab", new[] { folder })
                        .Select(AssetDatabase.GUIDToAssetPath)
                        .Where(p => Path.GetDirectoryName(p).Replace('\\', '/') == folder)
                        .OrderBy(p => Path.GetFileNameWithoutExtension(p), System.StringComparer.Ordinal)
                        .ToList();
                    if (prefabPaths.Count != 10) Debug.LogWarning($"SortThem: {def.Folder} has {prefabPaths.Count} prefabs, expected 10");

                    for (int i = 0; i < prefabPaths.Count; i++)
                    {
                        var src = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPaths[i]);
                        string carId = "car_" + def.Id + "_" + i.ToString("00");
                        EditorUtility.DisplayProgressBar("Importing ARCADE cars", carId + " (" + src.name + ")", (c * 10 + i) / 150f);
                        var mesh = BuildMesh(src, carId, materials, out var mats, out var bounds, out int t);
                        tris += t;
                        mesh = SaveMesh(mesh, ArcadeMeshes + "/" + carId + ".asset");
                        var prefab = SavePrefab(carId, mesh, mats, bounds, Paths.CarPrefabs + "/" + carId + ".prefab");
                        var data = EditorAssets.LoadOrCreate<CarItemData>(Paths.Cars + "/" + carId + ".asset");
                        data.CarID = carId;
                        data.DevName = src.name;
                        data.Category = cat;
                        data.Prefab = prefab;
                        data.DisplayPrice = 2.5f + ((c * 10 + i) * 7 % 40) + (i % 2) * 0.5f;
                        LocUtil.Set("car." + carId, src.name, src.name);
                        data.DisplayName = LocUtil.Ref("car." + carId);
                        EditorUtility.SetDirty(data);
                        cars.Add(data);
                        total++;
                    }
                }
            }
            finally { EditorUtility.ClearProgressBar(); }

            catalog.Categories = categories.ToArray();
            catalog.Cars = cars.ToArray();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"SortThem: imported {total} ARCADE cars in {categories.Count} categories, {materials.Count} materials, avg {(total > 0 ? tris / total : 0)} tris");
        }

        static void ClearFolder(string folder, string pattern)
        {
            if (!Directory.Exists(folder)) return;
            foreach (var f in Directory.GetFiles(folder, pattern)) AssetDatabase.DeleteAsset(f.Replace('\\', '/'));
        }

        static Mesh BuildMesh(GameObject src, string name, Dictionary<Material, Material> materials, out Material[] mats, out Bounds bounds, out int tris)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(src);
            try
            {
                go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                go.transform.localScale = Vector3.one;
                var filters = go.GetComponentsInChildren<MeshFilter>(true).Where(f => f.sharedMesh != null && f.GetComponent<MeshRenderer>() != null).ToList();
                bool has = false; var wb = new Bounds();
                foreach (var f in filters) { var b = f.GetComponent<Renderer>().bounds; if (!has) { wb = b; has = true; } else wb.Encapsulate(b); }
                float s = Mathf.Min(TargetScale, MaxLength / Mathf.Max(wb.size.x, wb.size.z), MaxHeight / wb.size.y);
                var root = Matrix4x4.TRS(-wb.center * s, Quaternion.identity, Vector3.one * s);

                var perMaterial = new Dictionary<Material, List<CombineInstance>>();
                var order = new List<Material>();
                foreach (var f in filters)
                {
                    var r = f.GetComponent<MeshRenderer>();
                    var m = f.sharedMesh;
                    for (int sub = 0; sub < m.subMeshCount; sub++)
                    {
                        var srcMat = sub < r.sharedMaterials.Length ? r.sharedMaterials[sub] : null;
                        var target = MapMaterial(srcMat, materials);
                        if (!perMaterial.TryGetValue(target, out var list)) { list = new List<CombineInstance>(); perMaterial[target] = list; order.Add(target); }
                        list.Add(new CombineInstance { mesh = m, subMeshIndex = sub, transform = root * f.transform.localToWorldMatrix });
                    }
                }
                var parts = new List<CombineInstance>();
                foreach (var target in order)
                {
                    var tmp = new Mesh();
                    tmp.CombineMeshes(perMaterial[target].ToArray(), true, true);
                    parts.Add(new CombineInstance { mesh = tmp, transform = Matrix4x4.identity });
                }
                var mesh = new Mesh { name = name };
                mesh.CombineMeshes(parts.ToArray(), false, false);
                foreach (var p in parts) UnityEngine.Object.DestroyImmediate(p.mesh);
                mesh.RecalculateBounds();
                mesh.Optimize();
                mesh.UploadMeshData(false);
                mats = order.ToArray();
                bounds = mesh.bounds;
                tris = (int)mesh.GetIndexCount(0) / 3;
                for (int sub = 1; sub < mesh.subMeshCount; sub++) tris += (int)mesh.GetIndexCount(sub) / 3;
                return mesh;
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        static Material MapMaterial(Material src, Dictionary<Material, Material> cache)
        {
            if (src == null) src = AssetDatabase.LoadAssetAtPath<Material>("Assets/Cars model/ARCADE - Ultimate Vehicles Pack/Materials/Color Variations/ColorVar1_Material.mat");
            if (cache.TryGetValue(src, out var ours)) return ours;
            string path = ArcadeMaterials + "/Arcade_" + src.name + ".mat";
            ours = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find("SortThem/TexturedLit");
            if (ours == null) { ours = new Material(shader); AssetDatabase.CreateAsset(ours, path); }
            else ours.shader = shader;
            var tex = src.HasProperty("_BaseMap") ? src.GetTexture("_BaseMap") : src.mainTexture;
            ours.SetTexture("_BaseMap", tex);
            ours.SetColor("_BaseColor", src.HasProperty("_BaseColor") ? src.GetColor("_BaseColor") : Color.white);
            ours.enableInstancing = true;
            EditorUtility.SetDirty(ours);
            cache[src] = ours;
            return ours;
        }

        static Mesh SaveMesh(Mesh mesh, string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(mesh, existing);
                existing.name = mesh.name;
                EditorUtility.SetDirty(existing);
                UnityEngine.Object.DestroyImmediate(mesh);
                return existing;
            }
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        static GameObject SavePrefab(string name, Mesh mesh, Material[] mats, Bounds bounds, string path)
        {
            var go = new GameObject(name);
            go.layer = LayerMask.NameToLayer("LooseItems");
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterials = mats;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.lightProbeUsage = LightProbeUsage.Off;
            mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
            var bc = go.AddComponent<BoxCollider>();
            bc.center = bounds.center;
            bc.size = bounds.size;
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 0.3f;
            rb.linearDamping = 0.05f;
            rb.angularDamping = 0.5f;
            rb.interpolation = RigidbodyInterpolation.None;
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
            go.AddComponent<CarInstance>();
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            UnityEngine.Object.DestroyImmediate(go);
            return prefab;
        }
    }
}

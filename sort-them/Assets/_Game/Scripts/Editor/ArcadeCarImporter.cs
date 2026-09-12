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
        const string ArcadeTextures = Paths.Root + "/Art/Textures";
        const string AtlasTexturePath = ArcadeTextures + "/Arcade_Atlas.png";
        const string AtlasMaterialPath = ArcadeMaterials + "/Arcade_Atlas.mat";
        const string LodFolder = PoolRoot + "/_LOD";
        const string DefaultMaterial = "Assets/Cars model/ARCADE - Ultimate Vehicles Pack/Materials/Color Variations/ColorVar1_Material.mat";
        static readonly string[] LodTags = { "LOD1", "LOD2" };
        const int AtlasSize = 2048, AtlasCell = 512;
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

        class Atlas
        {
            public Material Material;
            public readonly Dictionary<Texture, Rect> Rects = new Dictionary<Texture, Rect>();
            public Texture Default;

            public Rect RectFor(Material src)
            {
                var tex = TextureOf(src) ?? Default;
                return tex != null && Rects.TryGetValue(tex, out var r) ? r : new Rect(0f, 0f, 1f, 1f);
            }
        }

        static Texture TextureOf(Material m)
        {
            if (m == null) return null;
            return m.HasProperty("_BaseMap") ? m.GetTexture("_BaseMap") : m.mainTexture;
        }

        [MenuItem("SortThem/3g. Import ARCADE Cars")]
        public static void Import()
        {
            EditorAssets.EnsureFolder(Paths.Categories);
            EditorAssets.EnsureFolder(Paths.Cars);
            EditorAssets.EnsureFolder(Paths.CarPrefabs);
            EditorAssets.EnsureFolder(Paths.Meshes);
            EditorAssets.EnsureFolder(ArcadeMaterials);
            EditorAssets.EnsureFolder(ArcadeMeshes);
            EditorAssets.EnsureFolder(ArcadeTextures);
            ClearFolder(Paths.Meshes, "car_*.asset");
            var written = new HashSet<string>();

            var catalog = EditorAssets.LoadOrCreate<CarCatalog>(Paths.Catalog);
            var categories = new List<CategoryData>();
            var cars = new List<CarItemData>();
            int total = 0, tris = 0, lodTris1 = 0, lodTris2 = 0, lodMissing = 0;
            try
            {
                var sources = new List<(int cat, int index, GameObject prefab)>();
                for (int c = 0; c < Categories.Length; c++)
                {
                    string folder = PoolRoot + "/" + Categories[c].Folder;
                    var prefabPaths = AssetDatabase.FindAssets("t:Prefab", new[] { folder })
                        .Select(AssetDatabase.GUIDToAssetPath)
                        .Where(p => Path.GetDirectoryName(p).Replace('\\', '/') == folder)
                        .OrderBy(p => Path.GetFileNameWithoutExtension(p), System.StringComparer.Ordinal)
                        .ToList();
                    if (prefabPaths.Count != 10) Debug.LogWarning($"SortThem: {Categories[c].Folder} has {prefabPaths.Count} prefabs, expected 10");
                    for (int i = 0; i < prefabPaths.Count; i++) sources.Add((c, i, AssetDatabase.LoadAssetAtPath<GameObject>(prefabPaths[i])));
                }

                EditorUtility.DisplayProgressBar("Importing ARCADE cars", "Building texture atlas", 0f);
                var atlas = BuildAtlas(sources.Select(s => s.prefab));
                written.Add(AtlasMaterialPath);

                foreach (var def in Categories)
                {
                    string catId = "cat_" + def.Id;
                    string catPath = Paths.Categories + "/" + catId + ".asset";
                    written.Add(catPath);
                    var cat = EditorAssets.LoadOrCreate<CategoryData>(catPath);
                    cat.CategoryID = catId;
                    cat.DevName = def.En;
                    cat.CategoryColor = def.Color;
                    LocUtil.Set("cat." + catId, def.Ru, def.En);
                    cat.DisplayName = LocUtil.Ref("cat." + catId);
                    EditorUtility.SetDirty(cat);
                    categories.Add(cat);
                }

                foreach (var (c, i, src) in sources)
                {
                    var def = Categories[c];
                    string carId = "car_" + def.Id + "_" + i.ToString("00");
                    EditorUtility.DisplayProgressBar("Importing ARCADE cars", carId + " (" + src.name + ")", (c * 10 + i) / 150f);
                    var mesh = BuildMesh(src, carId, null, (f, sub) => SubMaterial(f, sub), atlas, out var bounds, out int t, out var root);
                    tris += t;
                    string meshPath = ArcadeMeshes + "/" + carId + ".asset";
                    written.Add(meshPath);
                    mesh = SaveMesh(mesh, meshPath);
                    var lods = new List<Mesh> { mesh };
                    var parts = SourceParts(src);
                    var subCache = new Dictionary<(Mesh, int), int>();
                    for (int l = 0; l < LodTags.Length; l++)
                    {
                        var lodSrc = LoadLodSource(src, LodTags[l]);
                        if (lodSrc == null) { lodMissing++; break; }
                        var lodMesh = BuildMesh(lodSrc, carId + "_" + LodTags[l], root, (f, sub) => LookupMaterial(parts, f, sub, subCache), atlas, out var lodBounds, out int lt, out _);
                        if (l == 0) lodTris1 += lt; else lodTris2 += lt;
                        float ratio = lodBounds.size.magnitude / Mathf.Max(0.001f, bounds.size.magnitude);
                        if (Mathf.Abs(ratio - 1f) > 0.05f) Debug.LogWarning($"SortThem: {carId} {LodTags[l]} bounds differ from LOD0 by {ratio:0.###}x");
                        string lodPath = ArcadeMeshes + "/" + carId + "_" + LodTags[l] + ".asset";
                        written.Add(lodPath);
                        lods.Add(SaveMesh(lodMesh, lodPath));
                    }
                    string prefabPath = Paths.CarPrefabs + "/" + carId + ".prefab";
                    written.Add(prefabPath);
                    var prefab = SavePrefab(carId, lods.ToArray(), atlas.Material, bounds, prefabPath);
                    string dataPath = Paths.Cars + "/" + carId + ".asset";
                    written.Add(dataPath);
                    var data = EditorAssets.LoadOrCreate<CarItemData>(dataPath);
                    data.CarID = carId;
                    data.DevName = src.name;
                    data.Category = categories[c];
                    data.Prefab = prefab;
                    data.DisplayPrice = 2.5f + ((c * 10 + i) * 7 % 40) + (i % 2) * 0.5f;
                    LocUtil.Set("car." + carId, src.name, src.name);
                    data.DisplayName = LocUtil.Ref("car." + carId);
                    EditorUtility.SetDirty(data);
                    cars.Add(data);
                    total++;
                }
            }
            finally { EditorUtility.ClearProgressBar(); }

            catalog.Categories = categories.ToArray();
            catalog.Cars = cars.ToArray();
            EditorUtility.SetDirty(catalog);
            DeleteStale(Paths.Cars, "*.asset", written);
            DeleteStale(Paths.CarPrefabs, "*.prefab", written);
            DeleteStale(Paths.Categories, "*.asset", written);
            DeleteStale(ArcadeMeshes, "*.asset", written);
            DeleteStale(ArcadeMaterials, "*.mat", written);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"SortThem: imported {total} ARCADE cars in {categories.Count} categories, 1 atlas material, avg {(total > 0 ? tris / total : 0)} tris, LOD1 avg {(total > 0 ? lodTris1 / total : 0)}, LOD2 avg {(total > 0 ? lodTris2 / total : 0)}, cars without LOD {lodMissing}");
        }

        static Atlas BuildAtlas(IEnumerable<GameObject> sources)
        {
            var atlas = new Atlas();
            var defaultMat = AssetDatabase.LoadAssetAtPath<Material>(DefaultMaterial);
            atlas.Default = TextureOf(defaultMat);
            var textures = new List<Texture>();
            if (atlas.Default != null) textures.Add(atlas.Default);
            foreach (var src in sources)
                foreach (var r in src.GetComponentsInChildren<MeshRenderer>(true))
                    foreach (var m in r.sharedMaterials)
                    {
                        var tex = TextureOf(m);
                        if (tex != null && !textures.Contains(tex)) textures.Add(tex);
                    }
            int perRow = AtlasSize / AtlasCell;
            if (textures.Count > perRow * perRow) throw new System.Exception($"SortThem: atlas too small for {textures.Count} textures");

            var pixels = new Texture2D(AtlasSize, AtlasSize, TextureFormat.RGB24, false, false);
            var rt = RenderTexture.GetTemporary(AtlasCell, AtlasCell, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var prevActive = RenderTexture.active;
            try
            {
                for (int i = 0; i < textures.Count; i++)
                {
                    int cx = (i % perRow) * AtlasCell, cy = (i / perRow) * AtlasCell;
                    Graphics.Blit(textures[i], rt);
                    RenderTexture.active = rt;
                    pixels.ReadPixels(new Rect(0, 0, AtlasCell, AtlasCell), cx, cy);
                    float pad = 0.5f / AtlasSize;
                    atlas.Rects[textures[i]] = new Rect((float)cx / AtlasSize + pad, (float)cy / AtlasSize + pad, (float)AtlasCell / AtlasSize - 2f * pad, (float)AtlasCell / AtlasSize - 2f * pad);
                }
            }
            finally
            {
                RenderTexture.active = prevActive;
                RenderTexture.ReleaseTemporary(rt);
            }
            pixels.Apply();
            File.WriteAllBytes(AtlasTexturePath, pixels.EncodeToPNG());
            Object.DestroyImmediate(pixels);
            AssetDatabase.ImportAsset(AtlasTexturePath, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(AtlasTexturePath);
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = AtlasSize;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.SaveAndReimport();
            var atlasTex = AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasTexturePath);

            var shader = Shader.Find("SortThem/TexturedLit");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(AtlasMaterialPath);
            if (mat == null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, AtlasMaterialPath); }
            else mat.shader = shader;
            mat.SetTexture("_BaseMap", atlasTex);
            mat.SetColor("_BaseColor", Color.white);
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            atlas.Material = mat;
            Debug.Log($"SortThem: atlas {AtlasSize}x{AtlasSize} from {textures.Count} textures");
            return atlas;
        }

        static void ClearFolder(string folder, string pattern)
        {
            if (!Directory.Exists(folder)) return;
            foreach (var f in Directory.GetFiles(folder, pattern)) AssetDatabase.DeleteAsset(f.Replace('\\', '/'));
        }

        static void DeleteStale(string folder, string pattern, HashSet<string> keep)
        {
            if (!Directory.Exists(folder)) return;
            foreach (var f in Directory.GetFiles(folder, pattern))
            {
                string p = f.Replace('\\', '/');
                if (!keep.Contains(p)) AssetDatabase.DeleteAsset(p);
            }
        }

        static GameObject LoadLodSource(GameObject src, string tag)
        {
            var mf = src.GetComponentsInChildren<MeshFilter>(true).FirstOrDefault(f => f.sharedMesh != null);
            if (mf == null) return null;
            string fbx = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(mf.sharedMesh));
            return AssetDatabase.LoadAssetAtPath<GameObject>(LodFolder + "/" + fbx + "_" + tag + ".fbx");
        }

        static Dictionary<string, MeshFilter> SourceParts(GameObject src)
        {
            var d = new Dictionary<string, MeshFilter>(System.StringComparer.OrdinalIgnoreCase);
            foreach (var f in src.GetComponentsInChildren<MeshFilter>(true))
                if (f.sharedMesh != null && f.GetComponent<MeshRenderer>() != null && !d.ContainsKey(f.gameObject.name)) d[f.gameObject.name] = f;
            return d;
        }

        static Material LookupMaterial(Dictionary<string, MeshFilter> parts, MeshFilter lod, int sub, Dictionary<(Mesh, int), int> cache)
        {
            if (!parts.TryGetValue(lod.gameObject.name, out var srcF)) return null;
            var mats = srcF.GetComponent<MeshRenderer>().sharedMaterials;
            if (mats == null || mats.Length == 0) return AssetDatabase.LoadAssetAtPath<Material>(DefaultMaterial);
            int origSub = 0;
            if (mats.Length > 1 && lod.sharedMesh.subMeshCount > 1)
            {
                if (!cache.TryGetValue((lod.sharedMesh, sub), out origSub))
                {
                    origSub = MatchSubmesh(lod.sharedMesh, sub, lod.transform.localToWorldMatrix, srcF.sharedMesh, srcF.transform.localToWorldMatrix);
                    cache[(lod.sharedMesh, sub)] = origSub;
                }
            }
            var m = mats[Mathf.Clamp(origSub, 0, mats.Length - 1)];
            return m != null ? m : AssetDatabase.LoadAssetAtPath<Material>(DefaultMaterial);
        }

        static int MatchSubmesh(Mesh lod, int sub, Matrix4x4 lodToWorld, Mesh orig, Matrix4x4 origToWorld)
        {
            const float uvWeight = 0.1f;
            var ov = orig.vertices; var ouv = orig.uv;
            var lv = lod.vertices; var luv = lod.uv;
            bool useUv = ouv != null && ouv.Length == ov.Length && luv != null && luv.Length == lv.Length;
            var ownerMask = new int[ov.Length];
            for (int s = 0; s < orig.subMeshCount; s++) foreach (var i in orig.GetTriangles(s)) ownerMask[i] |= 1 << s;
            var opos = new Vector3[ov.Length];
            for (int i = 0; i < ov.Length; i++) opos[i] = origToWorld.MultiplyPoint3x4(ov[i]);
            var votes = new int[orig.subMeshCount];
            var seen = new HashSet<int>();
            foreach (var vi in lod.GetTriangles(sub))
            {
                if (!seen.Add(vi)) continue;
                var p = lodToWorld.MultiplyPoint3x4(lv[vi]);
                var uv = useUv ? luv[vi] : Vector2.zero;
                float best = float.MaxValue; int bi = -1;
                for (int j = 0; j < opos.Length; j++)
                {
                    float d = (opos[j] - p).sqrMagnitude;
                    if (useUv) d += (ouv[j] - uv).sqrMagnitude * uvWeight * uvWeight;
                    if (d < best) { best = d; bi = j; }
                }
                if (bi < 0) continue;
                int mask = ownerMask[bi];
                for (int s = 0; s < votes.Length; s++) if ((mask & (1 << s)) != 0) votes[s]++;
            }
            int result = 0;
            for (int s = 1; s < votes.Length; s++) if (votes[s] > votes[result]) result = s;
            return result;
        }

        static Material SubMaterial(MeshFilter f, int sub)
        {
            var r = f.GetComponent<MeshRenderer>();
            var m = r != null && sub < r.sharedMaterials.Length ? r.sharedMaterials[sub] : null;
            return m != null ? m : AssetDatabase.LoadAssetAtPath<Material>(DefaultMaterial);
        }

        static Mesh BuildMesh(GameObject src, string name, Matrix4x4? fixedRoot, System.Func<MeshFilter, int, Material> srcMatFor, Atlas atlas, out Bounds bounds, out int tris, out Matrix4x4 root)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(src);
            var parts = new List<CombineInstance>();
            try
            {
                go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                go.transform.localScale = Vector3.one;
                var filters = go.GetComponentsInChildren<MeshFilter>(true).Where(f => f.sharedMesh != null && f.GetComponent<MeshRenderer>() != null).ToList();
                if (fixedRoot.HasValue) root = fixedRoot.Value;
                else
                {
                    bool has = false; var wb = new Bounds();
                    foreach (var f in filters) { var b = f.GetComponent<Renderer>().bounds; if (!has) { wb = b; has = true; } else wb.Encapsulate(b); }
                    float s = Mathf.Min(TargetScale, MaxLength / Mathf.Max(wb.size.x, wb.size.z), MaxHeight / wb.size.y);
                    root = Matrix4x4.TRS(-wb.center * s, Quaternion.identity, Vector3.one * s);
                }

                foreach (var f in filters)
                {
                    var m = f.sharedMesh;
                    for (int sub = 0; sub < m.subMeshCount; sub++)
                    {
                        var srcMat = srcMatFor(f, sub);
                        if (srcMat == null) continue;
                        var part = ExtractSubmesh(m, sub, atlas.RectFor(srcMat));
                        if (part == null) continue;
                        parts.Add(new CombineInstance { mesh = part, transform = root * f.transform.localToWorldMatrix });
                    }
                }
                var mesh = new Mesh { name = name };
                mesh.CombineMeshes(parts.ToArray(), true, true);
                mesh.RecalculateBounds();
                mesh.Optimize();
                mesh.UploadMeshData(false);
                bounds = mesh.bounds;
                tris = (int)mesh.GetIndexCount(0) / 3;
                return mesh;
            }
            finally
            {
                foreach (var p in parts) Object.DestroyImmediate(p.mesh);
                Object.DestroyImmediate(go);
            }
        }

        static Mesh ExtractSubmesh(Mesh m, int sub, Rect uvRect)
        {
            var tri = m.GetTriangles(sub);
            if (tri.Length == 0) return null;
            var v = m.vertices; var n = m.normals; var uv = m.uv;
            bool hasN = n != null && n.Length == v.Length, hasUv = uv != null && uv.Length == v.Length;
            var map = new Dictionary<int, int>();
            var nv = new List<Vector3>(); var nn = new List<Vector3>(); var nuv = new List<Vector2>();
            var nt = new int[tri.Length];
            for (int i = 0; i < tri.Length; i++)
            {
                int src = tri[i];
                if (!map.TryGetValue(src, out int dst))
                {
                    dst = nv.Count;
                    map[src] = dst;
                    nv.Add(v[src]);
                    if (hasN) nn.Add(n[src]);
                    var t = hasUv ? uv[src] : Vector2.zero;
                    t = new Vector2(Mathf.Clamp01(t.x), Mathf.Clamp01(t.y));
                    nuv.Add(uvRect.min + Vector2.Scale(t, uvRect.size));
                }
                nt[i] = dst;
            }
            var part = new Mesh();
            part.indexFormat = nv.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            part.SetVertices(nv);
            if (hasN) part.SetNormals(nn);
            part.SetUVs(0, nuv);
            part.SetTriangles(nt, 0);
            if (!hasN) part.RecalculateNormals();
            return part;
        }

        static Mesh SaveMesh(Mesh mesh, string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null)
            {
                existing.Clear(false);
                existing.indexFormat = mesh.indexFormat;
                existing.SetVertices(mesh.vertices);
                existing.SetNormals(mesh.normals);
                existing.SetUVs(0, mesh.uv);
                existing.SetTriangles(mesh.triangles, 0);
                existing.RecalculateBounds();
                existing.UploadMeshData(false);
                existing.name = mesh.name;
                EditorUtility.SetDirty(existing);
                Object.DestroyImmediate(mesh);
                return existing;
            }
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        static GameObject SavePrefab(string name, Mesh[] lods, Material mat, Bounds bounds, string path)
        {
            var go = new GameObject(name);
            go.layer = LayerMask.NameToLayer("LooseItems");
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = lods[0];
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
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
            go.AddComponent<CarInstance>().Lods = lods;
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }
    }
}

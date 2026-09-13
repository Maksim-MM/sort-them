using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SortThem.Editor
{
    public static class SpecialCarSetup
    {
        const string SourceRoot = "Assets/Cars model/Modular_Cyber_Racing_Cars";
        const string SourcePrefabs = SourceRoot + "/Prefabs";
        const string DataFolder = Paths.Data + "/Special";

        static readonly (string Car, string Wheels, string Ru, string En)[] Cars =
        {
            ("Car_01", "Wheel_05", "Элеанор", "Eleanor"),
            ("Car_03", "Wheel_04", "Саншайн", "Sunshine"),
            ("Car_07", "Wheel_10", "Рассердимся", "Watch Out"),
            ("Car_08", null, "Блюзмобиль", "Bluesmobile"),
            ("Car_11", "Wheel_09", "Мэтр", "Mater"),
        };

        static readonly (string Part, string Key, string Ru, string En)[] Steps =
        {
            ("Headlight", "headlight", "Фары", "Headlights"),
            ("FrontBumper", "front_bumper", "Передний бампер", "Front bumper"),
            ("RearBumper", "rear_bumper", "Задний бампер", "Rear bumper"),
            ("Pipe", "pipe", "Выхлоп", "Exhaust"),
            ("FogLight", "foglight", "Противотуманки", "Roof lights"),
            ("Engine", "engine", "Двигатель", "Engine"),
            ("Decals", "decals", "Декали", "Decals"),
            ("Spoiler", "spoiler", "Спойлер", "Spoiler"),
            ("Turbine", "turbine", "Турбина", "Turbine"),
        };

        static readonly string[] WheelSuffixes = { "Wheel_FL", "Wheel_FR", "Wheel_BL", "Wheel_BR" };

        [MenuItem("SortThem/3h. Import Special Cars")]
        public static void Import()
        {
            CreateBasePrefabs();
            ArcadeCarImporter.Import();
            CreateData();
            ShrinkTextures();
        }

        [MenuItem("SortThem/3i. Apply Special Car Names")]
        public static void ApplyNames()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CarCatalog>(Paths.Catalog);
            if (catalog == null || catalog.SpecialCategory == null) { Debug.LogError("SortThem: import special cars first (menu 3h)"); return; }
            int n = 0;
            foreach (var (car, _, ru, en) in Cars)
            {
                var item = catalog.Cars.FirstOrDefault(c => c != null && c.Category == catalog.SpecialCategory && c.DevName == car);
                if (item == null) continue;
                LocUtil.Set("car." + item.CarID, ru, en);
                n++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log("SortThem: special car names applied: " + n);
        }

        static GameObject PartPrefab(string car, string part) => AssetDatabase.LoadAssetAtPath<GameObject>(SourcePrefabs + "/" + car + "/" + car + "_" + part + ".prefab");
        static GameObject WheelPrefab(string set) => AssetDatabase.LoadAssetAtPath<GameObject>(SourcePrefabs + "/Wheels/" + set + ".prefab");

        static GameObject Reference(string car)
        {
            var full = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePrefabs + "/" + car + "/" + car + ".prefab");
            if (full != null && full.transform.childCount > 0) return full;
            return PartPrefab(car, "Body");
        }

        static (Vector3 pos, Vector3 euler) Pose(string car, GameObject part)
        {
            var reference = Reference(car);
            var t = reference != null ? reference.transform.Find(part.name) : null;
            return t != null ? (t.localPosition, t.localEulerAngles) : (Vector3.zero, Vector3.zero);
        }

        static SpecialCarData.Part Part(string car, GameObject prefab)
        {
            var (pos, euler) = Pose(car, prefab);
            return new SpecialCarData.Part { Prefab = prefab, LocalPosition = pos, LocalEuler = euler };
        }

        static IEnumerable<GameObject> BaseParts(string car)
        {
            yield return PartPrefab(car, "Body");
            yield return PartPrefab(car, "Steering_wheel");
            yield return PartPrefab(car, "FrontBumper_Simple");
            yield return PartPrefab(car, "RearBumper_Simple");
            yield return PartPrefab(car, "Pipe_Simple");
            yield return PartPrefab(car, "Headlight_Simple");
            foreach (var w in WheelSuffixes) yield return PartPrefab(car, w);
        }

        static void CreateBasePrefabs()
        {
            EditorAssets.EnsureFolder(ArcadeCarImporter.SpecialFolder);
            foreach (var (car, _, _, _) in Cars)
            {
                var root = new GameObject(car);
                foreach (var part in BaseParts(car))
                {
                    if (part == null) { Debug.LogError("SortThem: missing base part for " + car); continue; }
                    var mf = part.GetComponent<MeshFilter>();
                    var mr = part.GetComponent<MeshRenderer>();
                    if (mf == null || mr == null) { Debug.LogError("SortThem: part without mesh on root: " + part.name); continue; }
                    var go = new GameObject(part.name);
                    go.transform.SetParent(root.transform, false);
                    var (pos, euler) = Pose(car, part);
                    go.transform.localPosition = pos;
                    go.transform.localEulerAngles = euler;
                    go.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
                    go.AddComponent<MeshRenderer>().sharedMaterials = mr.sharedMaterials;
                }
                PrefabUtility.SaveAsPrefabAsset(root, ArcadeCarImporter.SpecialFolder + "/" + car + ".prefab");
                Object.DestroyImmediate(root);
            }
            AssetDatabase.SaveAssets();
        }

        static void CreateData()
        {
            EditorAssets.EnsureFolder(DataFolder);
            var catalog = AssetDatabase.LoadAssetAtPath<CarCatalog>(Paths.Catalog);
            if (catalog == null || catalog.SpecialCategory == null) { Debug.LogError("SortThem: catalog has no special category, run import first"); return; }
            foreach (var (key, ru, en) in Steps.Select(s => (s.Key, s.Ru, s.En)).Concat(new[] { ("wheels", "Колёса", "Wheels"), ("policelight", "Мигалка", "Police light") }))
                LocUtil.Set("special.step." + key, ru, en);

            var list = new List<SpecialCarData>();
            foreach (var (car, wheels, ru, en) in Cars)
            {
                var item = catalog.Cars.FirstOrDefault(c => c != null && c.Category == catalog.SpecialCategory && c.DevName == car);
                if (item == null) { Debug.LogError("SortThem: special car not imported: " + car); continue; }
                LocUtil.Set("car." + item.CarID, ru, en);
                var data = EditorAssets.LoadOrCreate<SpecialCarData>(DataFolder + "/" + car + ".asset");
                data.Car = item;
                data.BaseParts = BaseParts(car).Where(p => p != null).Select(p => Part(car, p)).ToArray();

                var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ArcadeCarImporter.SpecialFolder + "/" + car + ".prefab");
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
                var wb = new Bounds(); bool has = false;
                foreach (var r in inst.GetComponentsInChildren<Renderer>()) { if (!has) { wb = r.bounds; has = true; } else wb.Encapsulate(r.bounds); }
                Object.DestroyImmediate(inst);
                var root = ArcadeCarImporter.FitRoot(wb);
                data.AssemblyOffset = root.GetPosition();
                data.AssemblyScale = root.lossyScale.x;

                var steps = new List<SpecialCarData.Step>();
                foreach (var (part, key, _, _) in Steps)
                {
                    var step = new SpecialCarData.Step { DevName = key, DisplayName = LocUtil.Ref("special.step." + key) };
                    var cool = PartPrefab(car, part + "_Cool");
                    if (cool != null)
                    {
                        step.Add = new[] { Part(car, cool) };
                        step.Remove = new[] { PartPrefab(car, part + "_Simple") };
                    }
                    else
                    {
                        var add = PartPrefab(car, part);
                        if (add == null) { Debug.LogWarning("SortThem: " + car + " has no part " + part); continue; }
                        step.Add = new[] { Part(car, add) };
                    }
                    steps.Add(step);
                }
                if (wheels != null)
                {
                    var shared = WheelPrefab(wheels);
                    var step = new SpecialCarData.Step { DevName = "wheels", DisplayName = LocUtil.Ref("special.step.wheels") };
                    var add = new List<SpecialCarData.Part>();
                    var remove = new List<GameObject>();
                    foreach (var suffix in WheelSuffixes)
                    {
                        var own = PartPrefab(car, suffix);
                        if (own == null || shared == null) continue;
                        var (pos, _) = Pose(car, own);
                        remove.Add(own);
                        add.Add(new SpecialCarData.Part { Prefab = shared, LocalPosition = pos, LocalEuler = new Vector3(0f, pos.x >= 0f ? 90f : 270f, 0f) });
                    }
                    step.Add = add.ToArray();
                    step.Remove = remove.ToArray();
                    steps.Add(step);
                }
                else
                {
                    var police = PartPrefab(car, "Policelight");
                    if (police != null) steps.Add(new SpecialCarData.Step { DevName = "policelight", DisplayName = LocUtil.Ref("special.step.policelight"), Add = new[] { Part(car, police) } });
                }
                data.Steps = steps.ToArray();
                EditorUtility.SetDirty(data);
                list.Add(data);
                Debug.Log("SortThem: special " + car + " -> " + item.CarID + ", steps " + steps.Count + ", scale " + data.AssemblyScale.ToString("0.####"));
            }
            catalog.Specials = list.ToArray();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
        }

        static void ShrinkTextures()
        {
            foreach (var (name, size) in new[] { ("Decals_01", 1024), ("Decals_2", 1024), ("Tire_Normal", 512) })
            {
                var path = SourceRoot + "/Textures/" + name + ".png";
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null || importer.maxTextureSize <= size) continue;
                importer.maxTextureSize = size;
                importer.SaveAndReimport();
            }
        }
    }
}

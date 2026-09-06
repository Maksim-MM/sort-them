using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SortThem.Editor
{
    public static class CarModelGenerator
    {
        struct Part
        {
            public Mesh Mesh;
            public Vector3 Pos;
            public Quaternion Rot;
            public Vector3 Scale;
            public Color Color;
        }

        class Family
        {
            public string Id, DevName, Ru, En;
            public Color Color;
            public float BaseLength;
            public string[] RuVariants, EnVariants;
            public Action<List<Part>, float, int, Color> Build;
        }

        static Mesh _cube, _cylinder, _sphere;

        static readonly Color Dark = new Color(0.12f, 0.12f, 0.13f);
        static readonly Color Glass = new Color(0.62f, 0.8f, 0.95f);
        static readonly Color Chrome = new Color(0.8f, 0.82f, 0.86f);
        static readonly Color Brown = new Color(0.45f, 0.3f, 0.15f);
        static readonly Color Red = new Color(0.9f, 0.15f, 0.1f);
        static readonly Color Blue = new Color(0.15f, 0.35f, 0.95f);
        static readonly Color Light = new Color(0.95f, 0.95f, 0.9f);

        static readonly Color[] Palette =
        {
            new Color(0.85f, 0.15f, 0.15f), new Color(0.15f, 0.3f, 0.85f), new Color(0.15f, 0.65f, 0.25f), new Color(0.95f, 0.85f, 0.1f),
            new Color(0.95f, 0.5f, 0.1f), new Color(0.55f, 0.2f, 0.75f), new Color(0.92f, 0.92f, 0.9f), new Color(0.2f, 0.2f, 0.22f),
            new Color(0.1f, 0.7f, 0.7f), new Color(0.95f, 0.45f, 0.7f)
        };
        static readonly string[] RuColors = { "Красный", "Синий", "Зелёный", "Жёлтый", "Оранжевый", "Фиолетовый", "Белый", "Чёрный", "Бирюзовый", "Розовый" };
        static readonly string[] EnColors = { "Red", "Blue", "Green", "Yellow", "Orange", "Purple", "White", "Black", "Teal", "Pink" };
        static readonly string[] RuLength = { "короткий", "", "длинный" };
        static readonly string[] EnLength = { "short", "", "long" };

        static readonly Family[] Families =
        {
            new Family { Id = "muscle", DevName = "Muscle", Ru = "Маслкары", En = "Muscle Cars", Color = new Color(0.85f, 0.2f, 0.2f), BaseLength = 0.42f, Build = Muscle,
                RuVariants = new[] { "маслкар", "маслкар со спойлером", "маслкар с воздухозаборником", "маслкар-кабриолет" },
                EnVariants = new[] { "muscle car", "muscle car with spoiler", "muscle car with hood scoop", "muscle convertible" } },
            new Family { Id = "supercars", DevName = "Supercar", Ru = "Суперкары", En = "Supercars", Color = new Color(0.95f, 0.5f, 0.1f), BaseLength = 0.44f, Build = Supercar,
                RuVariants = new[] { "суперкар", "суперкар с антикрылом", "суперкар с воздухозаборником", "суперкар с плавниками" },
                EnVariants = new[] { "supercar", "supercar with rear wing", "supercar with roof scoop", "supercar with fins" } },
            new Family { Id = "trucks", DevName = "Truck", Ru = "Грузовики", En = "Trucks", Color = new Color(0.3f, 0.4f, 0.7f), BaseLength = 0.5f, Build = Truck,
                RuVariants = new[] { "грузовик-контейнеровоз", "бензовоз", "лесовоз", "самосвал" },
                EnVariants = new[] { "container truck", "tanker truck", "log truck", "dump truck" } },
            new Family { Id = "emergency", DevName = "Emergency", Ru = "Спецслужбы", En = "Emergency", Color = new Color(0.2f, 0.5f, 0.9f), BaseLength = 0.44f, Build = Emergency,
                RuVariants = new[] { "полицейский седан", "фургон скорой помощи", "пожарный автомобиль", "внедорожник спецслужб" },
                EnVariants = new[] { "police sedan", "ambulance van", "fire truck", "emergency SUV" } },
            new Family { Id = "construction", DevName = "Construction", Ru = "Стройтехника", En = "Construction", Color = new Color(0.95f, 0.8f, 0.1f), BaseLength = 0.42f, Build = Construction,
                RuVariants = new[] { "экскаватор", "бульдозер", "автокран", "дорожный каток" },
                EnVariants = new[] { "excavator", "bulldozer", "mobile crane", "road roller" } },
            new Family { Id = "buses", DevName = "Bus", Ru = "Автобусы", En = "Buses", Color = new Color(0.2f, 0.7f, 0.4f), BaseLength = 0.55f, Build = Bus,
                RuVariants = new[] { "городской автобус", "двухэтажный автобус", "школьный автобус", "автобус с багажником" },
                EnVariants = new[] { "city bus", "double-decker bus", "school bus", "bus with roof rack" } },
            new Family { Id = "pickups", DevName = "Pickup", Ru = "Пикапы и фургоны", En = "Pickups & Vans", Color = new Color(0.55f, 0.35f, 0.2f), BaseLength = 0.44f, Build = Pickup,
                RuVariants = new[] { "пикап", "фургон", "пикап с кунгом", "фургон с рейлингами" },
                EnVariants = new[] { "pickup truck", "panel van", "pickup with camper shell", "van with roof rails" } },
            new Family { Id = "racing", DevName = "Racing", Ru = "Гонки", En = "Racing", Color = new Color(0.7f, 0.2f, 0.7f), BaseLength = 0.5f, Build = Racing,
                RuVariants = new[] { "гоночный болид", "болид с большим антикрылом", "болид с воздухозаборником", "болид с двойным носом" },
                EnVariants = new[] { "racing car", "racing car with big wing", "racing car with airbox", "twin-nose racing car" } },
            new Family { Id = "retro", DevName = "Retro", Ru = "Ретро", En = "Retro", Color = new Color(0.6f, 0.5f, 0.3f), BaseLength = 0.4f, Build = Retro,
                RuVariants = new[] { "ретро-седан", "ретро-родстер", "ретро-универсал", "ретро-седан с багажом" },
                EnVariants = new[] { "retro sedan", "retro roadster", "retro station wagon", "retro sedan with luggage" } },
            new Family { Id = "monster", DevName = "Monster", Ru = "Монстр-траки", En = "Monster Trucks", Color = new Color(0.1f, 0.7f, 0.7f), BaseLength = 0.42f, Build = Monster,
                RuVariants = new[] { "монстр-трак", "монстр-фургон", "монстр-трак с флагами", "монстр-трак с фарами" },
                EnVariants = new[] { "monster truck", "monster van", "monster truck with flags", "monster truck with light bar" } },
        };

        [MenuItem("SortThem/3. Generate Cars")]
        public static void Generate()
        {
            LoadPrimitives();
            EditorAssets.EnsureFolder(Paths.Categories);
            EditorAssets.EnsureFolder(Paths.Cars);
            EditorAssets.EnsureFolder(Paths.CarPrefabs);
            EditorAssets.EnsureFolder(Paths.Meshes);
            var material = EditorAssets.LoadOrCreateMaterial("Cars", "SortThem/VertexColorLit", Color.white);
            var catalog = EditorAssets.LoadOrCreate<CarCatalog>(Paths.Catalog);
            var categories = new List<CategoryData>();
            var cars = new List<CarItemData>();

            try
            {
                for (int f = 0; f < Families.Length; f++)
                {
                    var fam = Families[f];
                    string catId = "cat_" + fam.Id;
                    var cat = EditorAssets.LoadOrCreate<CategoryData>(Paths.Categories + "/" + catId + ".asset");
                    cat.CategoryID = catId;
                    cat.DevName = fam.En;
                    cat.CategoryColor = fam.Color;
                    LocUtil.Set("cat." + catId, fam.Ru, fam.En);
                    cat.DisplayName = LocUtil.Ref("cat." + catId);
                    EditorUtility.SetDirty(cat);
                    categories.Add(cat);

                    for (int i = 0; i < 10; i++)
                    {
                        EditorUtility.DisplayProgressBar("Generating cars", fam.DevName + " " + i, (f * 10 + i) / 100f);
                        int len = i == 9 ? 1 : i % 3;
                        int acc = i == 9 ? 3 : i / 3;
                        float L = fam.BaseLength * (len == 0 ? 0.75f : len == 1 ? 1f : 1.3f);
                        var body = Palette[i];
                        var parts = new List<Part>();
                        fam.Build(parts, L, acc, body);

                        string carId = "car_" + fam.Id + "_" + i.ToString("00");
                        string devName = EnColors[i] + " " + (EnLength[len].Length > 0 ? EnLength[len] + " " : "") + fam.EnVariants[acc];
                        string ruName = RuColors[i] + " " + (RuLength[len].Length > 0 ? RuLength[len] + " " : "") + fam.RuVariants[acc];
                        devName = Capitalize(devName);

                        var mesh = Combine(parts, carId, out var bounds);
                        mesh = SaveMesh(mesh, Paths.Meshes + "/" + carId + ".asset");
                        var prefab = SavePrefab(carId, mesh, material, bounds, Paths.CarPrefabs + "/" + carId + ".prefab");

                        var data = EditorAssets.LoadOrCreate<CarItemData>(Paths.Cars + "/" + carId + ".asset");
                        data.CarID = carId;
                        data.DevName = devName;
                        data.Category = cat;
                        data.Prefab = prefab;
                        data.DisplayPrice = 2.5f + ((f * 10 + i) * 7 % 40) + (i % 2) * 0.5f;
                        LocUtil.Set("car." + carId, ruName, devName);
                        data.DisplayName = LocUtil.Ref("car." + carId);
                        EditorUtility.SetDirty(data);
                        cars.Add(data);
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            catalog.Categories = categories.ToArray();
            catalog.Cars = cars.ToArray();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("SortThem: generated " + cars.Count + " car models in " + categories.Count + " categories");
        }

        static string Capitalize(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1);

        static void LoadPrimitives()
        {
            _cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            _cylinder = Resources.GetBuiltinResource<Mesh>("New-Cylinder.fbx");
            _sphere = Resources.GetBuiltinResource<Mesh>("New-Sphere.fbx");
        }

        static Mesh SaveMesh(Mesh mesh, string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null)
            {
                existing.Clear();
                EditorUtility.CopySerialized(mesh, existing);
                existing.name = mesh.name;
                EditorUtility.SetDirty(existing);
                UnityEngine.Object.DestroyImmediate(mesh);
                return existing;
            }
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        static GameObject SavePrefab(string name, Mesh mesh, Material material, Bounds bounds, string path)
        {
            var go = new GameObject(name);
            go.layer = LayerMask.NameToLayer("LooseItems");
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = material;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = true;
            mr.lightProbeUsage = LightProbeUsage.Off;
            mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
            var bc = go.AddComponent<BoxCollider>();
            bc.center = Vector3.zero;
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

        const float MaxHeight = 0.32f, MaxLength = 0.42f;

        static Mesh Combine(List<Part> parts, string name, out Bounds bounds)
        {
            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var colors = new List<Color>();
            var tris = new List<int>();
            foreach (var p in parts)
            {
                var m = Matrix4x4.TRS(p.Pos, p.Rot, p.Scale);
                var nm = m.inverse.transpose;
                int baseIndex = verts.Count;
                var pv = p.Mesh.vertices;
                var pn = p.Mesh.normals;
                for (int i = 0; i < pv.Length; i++)
                {
                    verts.Add(m.MultiplyPoint3x4(pv[i]));
                    normals.Add(nm.MultiplyVector(pn[i]).normalized);
                    colors.Add(p.Color);
                }
                var pt = p.Mesh.triangles;
                for (int i = 0; i < pt.Length; i++) tris.Add(pt[i] + baseIndex);
            }
            var min = Vector3.positiveInfinity;
            var max = Vector3.negativeInfinity;
            foreach (var v in verts)
            {
                min = Vector3.Min(min, v);
                max = Vector3.Max(max, v);
            }
            var center = (min + max) * 0.5f;
            float fit = Mathf.Min(1f, MaxHeight / Mathf.Max(0.001f, max.y - min.y), MaxLength / Mathf.Max(0.001f, max.z - min.z));
            for (int i = 0; i < verts.Count; i++) verts[i] = (verts[i] - center) * fit;
            bounds = new Bounds(Vector3.zero, (max - min) * fit);

            var mesh = new Mesh { name = name };
            mesh.indexFormat = verts.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetColors(colors);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            mesh.UploadMeshData(false);
            return mesh;
        }

        static void Box(List<Part> p, float x, float y, float z, float sx, float sy, float sz, Color c, float rx = 0f, float ry = 0f, float rz = 0f)
        {
            p.Add(new Part { Mesh = _cube, Pos = new Vector3(x, y, z), Rot = Quaternion.Euler(rx, ry, rz), Scale = new Vector3(sx, sy, sz), Color = c });
        }

        static void Cyl(List<Part> p, float x, float y, float z, float radius, float length, char axis, Color c)
        {
            var rot = axis == 'X' ? Quaternion.Euler(0f, 0f, 90f) : axis == 'Z' ? Quaternion.Euler(90f, 0f, 0f) : Quaternion.identity;
            p.Add(new Part { Mesh = _cylinder, Pos = new Vector3(x, y, z), Rot = rot, Scale = new Vector3(radius * 2f, length * 0.5f, radius * 2f), Color = c });
        }

        static void Sphere(List<Part> p, float x, float y, float z, float radius, Color c)
        {
            p.Add(new Part { Mesh = _sphere, Pos = new Vector3(x, y, z), Rot = Quaternion.identity, Scale = Vector3.one * radius * 2f, Color = c });
        }

        static void Wheel(List<Part> p, float x, float y, float z, float r, float w)
        {
            Cyl(p, x, y, z, r, w, 'X', Dark);
            Cyl(p, x, y, z, r * 0.5f, w * 1.15f, 'X', Chrome);
        }

        static void Wheels4(List<Part> p, float halfW, float zFront, float zRear, float r, float w)
        {
            Wheel(p, -halfW, r, zFront, r, w);
            Wheel(p, halfW, r, zFront, r, w);
            Wheel(p, -halfW, r, zRear, r, w);
            Wheel(p, halfW, r, zRear, r, w);
        }

        static void GlassBand(List<Part> p, float y, float z, float w, float h, float l) => Box(p, 0f, y, z, w, h, l, Glass);

        static void LightBar(List<Part> p, float y, float z, float w)
        {
            Box(p, -w * 0.15f, y, z, w * 0.28f, 0.03f, 0.05f, Red);
            Box(p, w * 0.15f, y, z, w * 0.28f, 0.03f, 0.05f, Blue);
        }

        static void Muscle(List<Part> p, float L, int acc, Color body)
        {
            float W = 0.19f, wr = 0.055f;
            Box(p, 0f, 0.105f, 0f, W, 0.09f, L, body);
            Box(p, 0f, 0.1f, L * 0.5f, W * 0.8f, 0.04f, 0.012f, Dark);
            if (acc != 3)
            {
                Box(p, 0f, 0.185f, -L * 0.08f, W * 0.86f, 0.07f, L * 0.42f, body);
                GlassBand(p, 0.185f, -L * 0.08f, W * 0.88f, 0.035f, L * 0.4f);
            }
            else
            {
                Box(p, 0f, 0.165f, L * 0.1f, W * 0.88f, 0.05f, 0.012f, Glass, -20f);
                Box(p, 0f, 0.155f, -L * 0.15f, W * 0.86f, 0.02f, L * 0.3f, Dark);
            }
            if (acc == 1)
            {
                Box(p, 0f, 0.215f, -L * 0.46f, W * 1.05f, 0.012f, 0.05f, Dark);
                Box(p, -W * 0.4f, 0.18f, -L * 0.46f, 0.015f, 0.06f, 0.02f, Dark);
                Box(p, W * 0.4f, 0.18f, -L * 0.46f, 0.015f, 0.06f, 0.02f, Dark);
            }
            if (acc == 2) Box(p, 0f, 0.165f, L * 0.28f, W * 0.35f, 0.04f, L * 0.2f, Dark);
            Wheels4(p, W * 0.5f, L * 0.32f, -L * 0.32f, wr, 0.05f);
        }

        static void Supercar(List<Part> p, float L, int acc, Color body)
        {
            float W = 0.21f, wr = 0.05f;
            Box(p, 0f, 0.075f, -L * 0.05f, W, 0.06f, L * 0.9f, body);
            Box(p, 0f, 0.055f, L * 0.4f, W * 0.95f, 0.035f, L * 0.25f, body);
            GlassBand(p, 0.125f, -L * 0.08f, W * 0.7f, 0.05f, L * 0.35f);
            Box(p, 0f, 0.13f, -L * 0.24f, W * 0.72f, 0.04f, L * 0.18f, body);
            if (acc == 1)
            {
                Box(p, 0f, 0.2f, -L * 0.44f, W * 1.1f, 0.012f, 0.06f, Dark);
                Box(p, -W * 0.35f, 0.16f, -L * 0.44f, 0.015f, 0.07f, 0.02f, Dark);
                Box(p, W * 0.35f, 0.16f, -L * 0.44f, 0.015f, 0.07f, 0.02f, Dark);
            }
            if (acc == 2) Box(p, 0f, 0.165f, -L * 0.1f, 0.05f, 0.04f, 0.08f, Dark);
            if (acc == 3)
            {
                Box(p, -W * 0.52f, 0.09f, -L * 0.3f, 0.02f, 0.07f, L * 0.3f, body);
                Box(p, W * 0.52f, 0.09f, -L * 0.3f, 0.02f, 0.07f, L * 0.3f, body);
            }
            Wheels4(p, W * 0.5f, L * 0.33f, -L * 0.33f, wr, 0.06f);
        }

        static void Truck(List<Part> p, float L, int acc, Color body)
        {
            float W = 0.22f, wr = 0.06f;
            var accent = Color.Lerp(body, Color.white, 0.5f);
            Box(p, 0f, 0.085f, -L * 0.05f, W * 0.8f, 0.03f, L * 0.95f, Dark);
            Box(p, 0f, 0.2f, L * 0.37f, W, 0.2f, L * 0.24f, body);
            GlassBand(p, 0.245f, L * 0.37f, W * 1.02f, 0.06f, L * 0.2f);
            Box(p, 0f, 0.1f, L * 0.5f, W, 0.03f, 0.02f, Chrome);
            switch (acc)
            {
                case 0:
                    Box(p, 0f, 0.22f, -L * 0.14f, W, 0.24f, L * 0.64f, accent);
                    break;
                case 1:
                    Cyl(p, 0f, 0.2f, -L * 0.14f, 0.1f, L * 0.62f, 'Z', Chrome);
                    Box(p, 0f, 0.11f, -L * 0.14f, W * 0.8f, 0.02f, L * 0.6f, Dark);
                    break;
                case 2:
                    Box(p, 0f, 0.11f, -L * 0.14f, W, 0.02f, L * 0.64f, Brown);
                    Cyl(p, -0.05f, 0.155f, -L * 0.14f, 0.035f, L * 0.6f, 'Z', Brown);
                    Cyl(p, 0.05f, 0.155f, -L * 0.14f, 0.035f, L * 0.6f, 'Z', Brown);
                    Cyl(p, 0f, 0.215f, -L * 0.14f, 0.035f, L * 0.6f, 'Z', Brown);
                    break;
                default:
                    Box(p, 0f, 0.2f, -L * 0.15f, W, 0.16f, L * 0.6f, accent, -10f);
                    break;
            }
            Wheel(p, -W * 0.5f, wr, L * 0.35f, wr, 0.05f);
            Wheel(p, W * 0.5f, wr, L * 0.35f, wr, 0.05f);
            Wheel(p, -W * 0.5f, wr, -L * 0.15f, wr, 0.05f);
            Wheel(p, W * 0.5f, wr, -L * 0.15f, wr, 0.05f);
            Wheel(p, -W * 0.5f, wr, -L * 0.36f, wr, 0.05f);
            Wheel(p, W * 0.5f, wr, -L * 0.36f, wr, 0.05f);
        }

        static void Emergency(List<Part> p, float L, int acc, Color body)
        {
            float W = 0.19f, wr = 0.055f;
            switch (acc)
            {
                case 0:
                    Box(p, 0f, 0.105f, 0f, W, 0.09f, L, body);
                    Box(p, 0f, 0.105f, 0f, W * 1.02f, 0.03f, L * 0.9f, Light);
                    Box(p, 0f, 0.185f, -L * 0.05f, W * 0.86f, 0.07f, L * 0.45f, body);
                    GlassBand(p, 0.185f, -L * 0.05f, W * 0.88f, 0.035f, L * 0.43f);
                    LightBar(p, 0.235f, -L * 0.02f, W);
                    Wheels4(p, W * 0.5f, L * 0.32f, -L * 0.32f, wr, 0.05f);
                    break;
                case 1:
                    Box(p, 0f, 0.15f, -L * 0.1f, W, 0.2f, L * 0.7f, Light);
                    Box(p, 0f, 0.12f, L * 0.36f, W, 0.12f, L * 0.25f, body);
                    GlassBand(p, 0.16f, L * 0.34f, W * 1.02f, 0.04f, L * 0.15f);
                    Box(p, -W * 0.505f, 0.15f, -L * 0.1f, 0.006f, 0.08f, 0.02f, Red);
                    Box(p, -W * 0.505f, 0.15f, -L * 0.1f, 0.006f, 0.02f, 0.08f, Red);
                    Box(p, W * 0.505f, 0.15f, -L * 0.1f, 0.006f, 0.08f, 0.02f, Red);
                    Box(p, W * 0.505f, 0.15f, -L * 0.1f, 0.006f, 0.02f, 0.08f, Red);
                    LightBar(p, 0.265f, L * 0.1f, W);
                    Wheels4(p, W * 0.5f, L * 0.3f, -L * 0.3f, wr, 0.05f);
                    break;
                case 2:
                    L *= 1.15f;
                    Box(p, 0f, 0.085f, 0f, W * 0.8f, 0.03f, L * 0.95f, Dark);
                    Box(p, 0f, 0.17f, L * 0.36f, W, 0.16f, L * 0.25f, body);
                    GlassBand(p, 0.21f, L * 0.36f, W * 1.02f, 0.05f, L * 0.2f);
                    Box(p, 0f, 0.15f, -L * 0.12f, W, 0.12f, L * 0.6f, body);
                    Box(p, 0f, 0.245f, -L * 0.05f, 0.06f, 0.015f, L * 0.7f, Chrome, -8f);
                    Box(p, -0.03f, 0.255f, -L * 0.05f, 0.01f, 0.03f, L * 0.7f, Chrome, -8f);
                    Box(p, 0.03f, 0.255f, -L * 0.05f, 0.01f, 0.03f, L * 0.7f, Chrome, -8f);
                    LightBar(p, 0.265f, L * 0.36f, W);
                    Wheel(p, -W * 0.5f, wr, L * 0.33f, wr, 0.05f);
                    Wheel(p, W * 0.5f, wr, L * 0.33f, wr, 0.05f);
                    Wheel(p, -W * 0.5f, wr, -L * 0.12f, wr, 0.05f);
                    Wheel(p, W * 0.5f, wr, -L * 0.12f, wr, 0.05f);
                    Wheel(p, -W * 0.5f, wr, -L * 0.33f, wr, 0.05f);
                    Wheel(p, W * 0.5f, wr, -L * 0.33f, wr, 0.05f);
                    break;
                default:
                    wr = 0.065f;
                    Box(p, 0f, 0.14f, 0f, W, 0.14f, L, body);
                    GlassBand(p, 0.235f, -L * 0.05f, W * 0.9f, 0.05f, L * 0.55f);
                    Box(p, 0f, 0.265f, -L * 0.05f, W * 0.9f, 0.02f, L * 0.55f, body);
                    LightBar(p, 0.29f, L * 0.05f, W);
                    Wheels4(p, W * 0.5f, L * 0.32f, -L * 0.32f, wr, 0.06f);
                    break;
            }
        }

        static void Construction(List<Part> p, float L, int acc, Color body)
        {
            float W = 0.22f, wr = 0.075f;
            Box(p, 0f, 0.1f, 0f, W, 0.06f, L * 0.7f, body);
            switch (acc)
            {
                case 0:
                    Box(p, 0f, 0.19f, -L * 0.08f, W * 0.85f, 0.12f, L * 0.45f, body);
                    Box(p, W * 0.2f, 0.2f, L * 0.05f, W * 0.35f, 0.1f, L * 0.2f, Glass);
                    Box(p, 0f, 0.28f, L * 0.3f, 0.05f, 0.05f, L * 0.55f, body, 35f);
                    Box(p, 0f, 0.2f, L * 0.62f, 0.04f, 0.04f, L * 0.35f, body, -60f);
                    Box(p, 0f, 0.05f, L * 0.72f, 0.12f, 0.08f, 0.08f, Dark);
                    Box(p, -W * 0.55f, 0.05f, 0f, 0.06f, 0.1f, L * 0.75f, Dark);
                    Box(p, W * 0.55f, 0.05f, 0f, 0.06f, 0.1f, L * 0.75f, Dark);
                    break;
                case 1:
                    Box(p, 0f, 0.2f, -L * 0.05f, W * 0.7f, 0.14f, L * 0.35f, body);
                    GlassBand(p, 0.23f, -L * 0.05f, W * 0.72f, 0.06f, L * 0.33f);
                    Box(p, 0f, 0.1f, L * 0.5f, W * 1.25f, 0.12f, 0.03f, Dark);
                    Box(p, -W * 0.55f, 0.12f, L * 0.25f, 0.02f, 0.02f, L * 0.5f, Dark);
                    Box(p, W * 0.55f, 0.12f, L * 0.25f, 0.02f, 0.02f, L * 0.5f, Dark);
                    Cyl(p, W * 0.25f, 0.3f, L * 0.1f, 0.01f, 0.1f, 'Y', Dark);
                    Box(p, -W * 0.55f, 0.05f, 0f, 0.06f, 0.1f, L * 0.75f, Dark);
                    Box(p, W * 0.55f, 0.05f, 0f, 0.06f, 0.1f, L * 0.75f, Dark);
                    break;
                case 2:
                    Box(p, 0f, 0.18f, L * 0.25f, W * 0.8f, 0.12f, L * 0.3f, body);
                    GlassBand(p, 0.21f, L * 0.25f, W * 0.82f, 0.05f, L * 0.28f);
                    Cyl(p, 0f, 0.15f, -L * 0.1f, 0.06f, 0.04f, 'Y', Dark);
                    Box(p, 0f, 0.3f, -L * 0.05f, 0.05f, 0.05f, L * 1.0f, body, -35f);
                    Wheels4(p, W * 0.5f, L * 0.3f, -L * 0.3f, wr, 0.06f);
                    break;
                default:
                    Box(p, 0f, 0.2f, -L * 0.15f, W * 0.8f, 0.14f, L * 0.35f, body);
                    GlassBand(p, 0.23f, -L * 0.15f, W * 0.82f, 0.06f, L * 0.33f);
                    Cyl(p, 0f, 0.08f, L * 0.3f, 0.08f, W * 1.1f, 'X', Chrome);
                    Box(p, 0f, 0.14f, L * 0.2f, W * 1.15f, 0.03f, L * 0.25f, body);
                    Wheel(p, -W * 0.5f, wr, -L * 0.3f, wr, 0.06f);
                    Wheel(p, W * 0.5f, wr, -L * 0.3f, wr, 0.06f);
                    break;
            }
        }

        static void Bus(List<Part> p, float L, int acc, Color body)
        {
            float W = 0.2f, wr = 0.05f;
            if (acc == 2)
            {
                Box(p, 0f, 0.17f, -L * 0.08f, W, 0.24f, L * 0.8f, body);
                Box(p, 0f, 0.11f, L * 0.42f, W * 0.9f, 0.12f, L * 0.2f, body);
                GlassBand(p, 0.23f, -L * 0.08f, W * 1.02f, 0.06f, L * 0.74f);
            }
            else
            {
                Box(p, 0f, 0.17f, 0f, W, 0.24f, L, body);
                GlassBand(p, 0.23f, 0f, W * 1.02f, 0.06f, L * 0.92f);
            }
            if (acc == 1)
            {
                Box(p, 0f, 0.35f, 0f, W, 0.12f, L * 0.96f, body);
                GlassBand(p, 0.37f, 0f, W * 1.02f, 0.05f, L * 0.9f);
            }
            if (acc == 3)
            {
                Box(p, 0f, 0.3f, 0f, W * 0.8f, 0.02f, L * 0.5f, Dark);
                Box(p, -W * 0.4f, 0.315f, 0f, 0.015f, 0.04f, L * 0.5f, Dark);
                Box(p, W * 0.4f, 0.315f, 0f, 0.015f, 0.04f, L * 0.5f, Dark);
            }
            Box(p, 0f, 0.08f, L * 0.5f, W, 0.04f, 0.015f, Dark);
            Box(p, 0f, 0.08f, -L * 0.5f, W, 0.04f, 0.015f, Dark);
            Wheels4(p, W * 0.5f - 0.01f, L * 0.35f, -L * 0.35f, wr, 0.04f);
        }

        static void Pickup(List<Part> p, float L, int acc, Color body)
        {
            float W = 0.2f, wr = 0.055f;
            if (acc == 0 || acc == 2)
            {
                Box(p, 0f, 0.16f, L * 0.12f, W, 0.14f, L * 0.38f, body);
                GlassBand(p, 0.2f, L * 0.12f, W * 1.02f, 0.05f, L * 0.3f);
                Box(p, 0f, 0.12f, L * 0.38f, W, 0.07f, L * 0.22f, body);
                Box(p, 0f, 0.11f, -L * 0.27f, W, 0.06f, L * 0.46f, body);
                Box(p, -W * 0.48f, 0.16f, -L * 0.27f, 0.02f, 0.05f, L * 0.46f, body);
                Box(p, W * 0.48f, 0.16f, -L * 0.27f, 0.02f, 0.05f, L * 0.46f, body);
                Box(p, 0f, 0.16f, -L * 0.49f, W, 0.05f, 0.02f, body);
                if (acc == 2)
                {
                    Box(p, 0f, 0.21f, -L * 0.27f, W, 0.1f, L * 0.44f, Color.Lerp(body, Color.white, 0.5f));
                    GlassBand(p, 0.225f, -L * 0.27f, W * 1.02f, 0.035f, L * 0.38f);
                }
            }
            else
            {
                Box(p, 0f, 0.17f, -L * 0.05f, W, 0.2f, L * 0.9f, body);
                Box(p, 0f, 0.12f, L * 0.42f, W, 0.1f, L * 0.16f, body);
                GlassBand(p, 0.22f, L * 0.2f, W * 1.02f, 0.05f, L * 0.15f);
                if (acc == 3)
                {
                    Cyl(p, -W * 0.35f, 0.285f, -L * 0.05f, 0.01f, L * 0.7f, 'Z', Chrome);
                    Cyl(p, W * 0.35f, 0.285f, -L * 0.05f, 0.01f, L * 0.7f, 'Z', Chrome);
                }
            }
            Wheels4(p, W * 0.5f, L * 0.3f, -L * 0.32f, wr, 0.05f);
        }

        static void Racing(List<Part> p, float L, int acc, Color body)
        {
            float wr = 0.05f, bw = 0.09f;
            Box(p, 0f, 0.06f, 0f, bw, 0.06f, L * 0.8f, body);
            if (acc == 3)
            {
                Box(p, -0.035f, 0.05f, L * 0.45f, 0.03f, 0.04f, L * 0.25f, body);
                Box(p, 0.035f, 0.05f, L * 0.45f, 0.03f, 0.04f, L * 0.25f, body);
            }
            else Box(p, 0f, 0.05f, L * 0.45f, 0.06f, 0.04f, L * 0.25f, body);
            Box(p, 0f, 0.03f, L * 0.52f, 0.26f, 0.01f, 0.05f, body);
            Box(p, -0.13f, 0.04f, L * 0.52f, 0.006f, 0.03f, 0.05f, Dark);
            Box(p, 0.13f, 0.04f, L * 0.52f, 0.006f, 0.03f, 0.05f, Dark);
            if (acc == 1)
            {
                Box(p, 0f, 0.17f, -L * 0.44f, 0.3f, 0.012f, 0.08f, body);
                Box(p, -0.15f, 0.14f, -L * 0.44f, 0.006f, 0.07f, 0.08f, Dark);
                Box(p, 0.15f, 0.14f, -L * 0.44f, 0.006f, 0.07f, 0.08f, Dark);
                Box(p, 0f, 0.12f, -L * 0.44f, 0.02f, 0.1f, 0.02f, Dark);
            }
            else
            {
                Box(p, 0f, 0.14f, -L * 0.44f, 0.22f, 0.01f, 0.05f, body);
                Box(p, 0f, 0.11f, -L * 0.44f, 0.02f, 0.06f, 0.02f, Dark);
            }
            Box(p, 0f, 0.1f, -L * 0.05f, 0.06f, 0.04f, L * 0.2f, Glass);
            Sphere(p, 0f, 0.12f, -L * 0.05f, 0.028f, Light);
            if (acc == 2) Box(p, 0f, 0.14f, -L * 0.15f, 0.05f, 0.07f, 0.1f, body);
            Cyl(p, 0f, wr, L * 0.32f, 0.01f, 0.28f, 'X', Dark);
            Cyl(p, 0f, wr, -L * 0.32f, 0.01f, 0.28f, 'X', Dark);
            Wheels4(p, 0.13f, L * 0.32f, -L * 0.32f, wr, 0.07f);
        }

        static void Retro(List<Part> p, float L, int acc, Color body)
        {
            float W = 0.17f, wr = 0.06f;
            float bodyLen = acc == 2 ? L * 0.65f : L * 0.55f;
            if (acc == 1)
            {
                Box(p, 0f, 0.11f, -L * 0.1f, W, 0.1f, bodyLen, body);
                Box(p, 0f, 0.185f, L * 0.05f, W * 0.9f, 0.06f, 0.01f, Glass, -15f);
                Box(p, 0f, 0.165f, -L * 0.15f, W * 0.8f, 0.02f, L * 0.25f, Dark);
            }
            else
            {
                Box(p, 0f, 0.14f, -L * 0.1f, W, 0.16f, bodyLen, body);
                GlassBand(p, 0.18f, -L * 0.1f, W * 1.02f, 0.05f, bodyLen * 0.9f);
            }
            Box(p, 0f, 0.1f, L * 0.3f, W * 0.85f, 0.08f, L * 0.32f, body);
            Box(p, 0f, 0.1f, L * 0.47f, W * 0.6f, 0.07f, 0.01f, Chrome);
            Sphere(p, -W * 0.35f, 0.14f, L * 0.45f, 0.025f, Chrome);
            Sphere(p, W * 0.35f, 0.14f, L * 0.45f, 0.025f, Chrome);
            float fx = W * 0.5f + 0.005f;
            Cyl(p, -fx, wr + 0.01f, L * 0.3f, wr + 0.02f, 0.03f, 'X', body);
            Cyl(p, fx, wr + 0.01f, L * 0.3f, wr + 0.02f, 0.03f, 'X', body);
            Cyl(p, -fx, wr + 0.01f, -L * 0.3f, wr + 0.02f, 0.03f, 'X', body);
            Cyl(p, fx, wr + 0.01f, -L * 0.3f, wr + 0.02f, 0.03f, 'X', body);
            Box(p, -W * 0.55f, 0.05f, 0f, 0.03f, 0.01f, L * 0.4f, Dark);
            Box(p, W * 0.55f, 0.05f, 0f, 0.03f, 0.01f, L * 0.4f, Dark);
            if (acc == 2)
            {
                Box(p, -W * 0.505f, 0.12f, -L * 0.1f, 0.006f, 0.08f, bodyLen * 0.9f, Brown);
                Box(p, W * 0.505f, 0.12f, -L * 0.1f, 0.006f, 0.08f, bodyLen * 0.9f, Brown);
            }
            else Cyl(p, 0f, 0.13f, -L * 0.1f - bodyLen * 0.5f - 0.01f, 0.05f, 0.03f, 'Z', Dark);
            if (acc == 3)
            {
                Box(p, 0f, 0.245f, -L * 0.1f, W * 0.6f, 0.05f, L * 0.25f, Brown);
                Box(p, 0f, 0.25f, -L * 0.1f, W * 0.62f, 0.02f, 0.02f, Dark);
            }
            Wheels4(p, W * 0.5f, L * 0.3f, -L * 0.3f, wr, 0.03f);
        }

        static void Monster(List<Part> p, float L, int acc, Color body)
        {
            float W = 0.2f, wr = 0.11f, ww = 0.09f;
            Box(p, 0f, 0.2f, 0f, W * 0.6f, 0.04f, L * 0.7f, Dark);
            if (acc == 1)
            {
                Box(p, 0f, 0.31f, 0f, W, 0.18f, L * 0.9f, body);
                GlassBand(p, 0.35f, L * 0.1f, W * 1.02f, 0.05f, L * 0.5f);
            }
            else
            {
                Box(p, 0f, 0.26f, 0f, W, 0.08f, L * 0.9f, body);
                Box(p, 0f, 0.34f, L * 0.1f, W * 0.9f, 0.08f, L * 0.35f, body);
                GlassBand(p, 0.35f, L * 0.1f, W * 0.92f, 0.04f, L * 0.32f);
                Box(p, -W * 0.47f, 0.32f, -L * 0.27f, 0.02f, 0.04f, L * 0.35f, body);
                Box(p, W * 0.47f, 0.32f, -L * 0.27f, 0.02f, 0.04f, L * 0.35f, body);
            }
            if (acc == 2)
            {
                Cyl(p, -W * 0.4f, 0.42f, -L * 0.4f, 0.005f, 0.2f, 'Y', Dark);
                Cyl(p, W * 0.4f, 0.42f, -L * 0.4f, 0.005f, 0.2f, 'Y', Dark);
                Box(p, -W * 0.4f + 0.02f, 0.5f, -L * 0.4f, 0.04f, 0.03f, 0.005f, Red);
                Box(p, W * 0.4f + 0.02f, 0.5f, -L * 0.4f, 0.04f, 0.03f, 0.005f, Red);
            }
            if (acc == 3)
            {
                for (int i = 0; i < 4; i++)
                    Cyl(p, -0.06f + i * 0.04f, 0.395f, L * 0.27f, 0.012f, 0.02f, 'Z', Light);
                Box(p, 0f, 0.395f, L * 0.26f, 0.16f, 0.01f, 0.01f, Dark);
            }
            Cyl(p, 0f, wr, L * 0.32f, 0.015f, W * 1.3f, 'X', Dark);
            Cyl(p, 0f, wr, -L * 0.32f, 0.015f, W * 1.3f, 'X', Dark);
            Wheels4(p, W * 0.65f, L * 0.32f, -L * 0.32f, wr, ww);
        }
    }
}

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SortThem.Editor
{
    public static class ScatterBaker
    {
        [MenuItem("SortThem/5. Bake Scatter Layout")]
        public static void Bake() => Bake(0);

        public static void Bake(int copiesPerModel)
        {
            var gm = Object.FindFirstObjectByType<GameManager>();
            if (gm == null || gm.Catalog == null || gm.Config == null)
            {
                Debug.LogError("SortThem: open Main scene with GameManager first");
                return;
            }
            if (gm.Config.Piles == null || gm.Config.Piles.Length == 0)
            {
                Debug.LogError("SortThem: GameConfig.Piles is empty, run menu 4g first");
                return;
            }
            var catalog = gm.Catalog;
            var shelves = Object.FindObjectsByType<ShelfController>(FindObjectsSortMode.None);
            int CopiesFor(CarItemData car)
            {
                if (catalog.IsSpecial(car)) return 1;
                if (copiesPerModel > 0) return copiesPerModel;
                int models = 0;
                foreach (var c in catalog.Cars) if (c != null && c.Category == car.Category) models++;
                int slots = 0;
                foreach (var s in shelves) if (!s.Locked && s.Rack != null && s.Rack.Category == car.Category) slots += s.Capacity;
                return models > 0 && slots > 0 ? slots / models : 10;
            }

            var rng = new System.Random(gm.Scatterer != null ? gm.Scatterer.Seed : 12345);
            var order = new List<int>();
            for (int i = 0; i < catalog.Cars.Length; i++)
            {
                int copies = CopiesFor(catalog.Cars[i]);
                for (int k = 0; k < copies; k++) order.Add(i);
            }
            for (int i = order.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }

            var prevMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;
            Physics.SyncTransforms();
            var root = new GameObject("__ScatterBake").transform;
            var blockers = BlockRacks(root);
            var spawned = new List<CarInstance>(order.Count);
            int total = order.Count;
            var zones = PileDrop.Assign(gm.Config, total, rng);
            int next = 0;
            int steps = 0;
            const int perStep = 2;
            const int maxSteps = 6000;
            const float dt = 0.02f;
            try
            {
                while (steps < maxSteps)
                {
                    for (int k = 0; k < perStep && next < total; k++, next++)
                    {
                        var car = CarSpawner.Spawn(catalog.Cars[order[next]], root, next);
                        PileDrop.Drop(car, gm.Config, zones[next], rng);
                        spawned.Add(car);
                    }
                    Physics.Simulate(dt);
                    steps++;
                    if (steps % 50 == 0)
                    {
                        EditorUtility.DisplayProgressBar("Baking scatter", "spawned " + next + "/" + total + ", step " + steps, next < total ? next / (float)total * 0.5f : 0.5f + Mathf.Min(0.5f, (steps - total / perStep) / 3000f));
                        if (next >= total && AllSleeping(spawned)) break;
                    }
                }

                int settleSteps = Settle(blockers, spawned, zones, gm, rng, dt);

                var layout = EditorAssets.LoadOrCreate<LevelLayoutData>(Paths.Layout);
                layout.Catalog = catalog;
                var entries = new LevelLayoutData.Entry[spawned.Count];
                var half = gm.Config.LevelHalfExtents;
                int rescued = 0;
                for (int i = 0; i < spawned.Count; i++)
                {
                    var t = spawned[i].transform;
                    var p = t.position;
                    if (p.y < gm.Config.FloorY - 0.2f || Mathf.Abs(p.x) > half.x || Mathf.Abs(p.z) > half.z)
                    {
                        p = gm.Config.UnstuckCenter + new Vector3((float)rng.NextDouble() * 2f - 1f, (float)rng.NextDouble(), (float)rng.NextDouble() * 2f - 1f);
                        rescued++;
                    }
                    entries[i] = new LevelLayoutData.Entry { CarIndex = order[i], Position = p, Rotation = t.rotation };
                }
                layout.Instances = entries;
                EditorUtility.SetDirty(layout);
                gm.Layout = layout;
                EditorUtility.SetDirty(gm);
                AssetDatabase.SaveAssets();
                Debug.Log("SortThem: baked " + entries.Length + " cars in " + steps + " + " + settleSteps + " steps (" + ((steps + settleSteps) * dt).ToString("0.0") + " s simulated), rescued " + rescued + ", all sleeping=" + AllSleeping(spawned));
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                Object.DestroyImmediate(root.gameObject);
                Physics.simulationMode = prevMode;
            }
        }

        [MenuItem("SortThem/5b. Bake Collectibles")]
        public static void BakeCollectibles()
        {
            var gm = Object.FindFirstObjectByType<GameManager>();
            var layout = AssetDatabase.LoadAssetAtPath<LevelLayoutData>(Paths.Layout);
            if (gm == null || gm.Config == null || layout == null || layout.CollectiblePrefab == null)
            {
                Debug.LogError("SortThem: need Main scene with GameManager, baked layout and crate prefab (menu 3e)");
                return;
            }
            int count = Mathf.Clamp(gm.Config.CollectiblesTotal, 1, 64);
            var rng = new System.Random((gm.Scatterer != null ? gm.Scatterer.Seed : 12345) + 777);
            var prevMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;
            var root = new GameObject("__CollectibleBake").transform;
            var blockers = BlockRacks(root);
            var bodies = new List<Rigidbody>(count);
            try
            {
                var cars = new List<CarInstance>();
                CarSpawner.SpawnFromLayout(layout, root, cars);
                for (int i = 0; i < count; i++)
                {
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(layout.CollectiblePrefab, root);
                    var rb = go.GetComponent<Rigidbody>();
                    PileDrop.DropBody(rb, gm.Config, PileDrop.PickZone(gm.Config, rng), rng);
                    bodies.Add(rb);
                }
                const float dt = 0.02f;
                int steps = 0;
                for (; steps < 3000; steps++)
                {
                    Physics.Simulate(dt);
                    if (steps % 25 == 0)
                    {
                        bool sleeping = true;
                        foreach (var b in bodies) if (!b.IsSleeping()) { sleeping = false; break; }
                        if (steps > 100 && sleeping) break;
                    }
                }
                foreach (var go in blockers) if (go != null) Object.DestroyImmediate(go);
                blockers.Clear();
                Physics.SyncTransforms();
                foreach (var b in bodies) b.WakeUp();
                foreach (var c in cars) { c.Body.isKinematic = false; c.Body.WakeUp(); }
                for (int i = 0; i < 3000; i++)
                {
                    Physics.Simulate(dt);
                    steps++;
                    if (i % 25 == 0)
                    {
                        bool sleeping = true;
                        foreach (var b in bodies) if (!b.IsSleeping()) { sleeping = false; break; }
                        if (i > 100 && sleeping && AllSleeping(cars)) break;
                    }
                }
                var half = gm.Config.LevelHalfExtents;
                var entries = new LevelLayoutData.PoseEntry[count];
                int rescued = 0;
                for (int i = 0; i < count; i++)
                {
                    var t = bodies[i].transform;
                    var pos = t.position;
                    if (pos.y < gm.Config.FloorY - 0.2f || Mathf.Abs(pos.x) > half.x || Mathf.Abs(pos.z) > half.z)
                    {
                        pos = gm.Config.UnstuckCenter + new Vector3((float)rng.NextDouble() * 2f - 1f, (float)rng.NextDouble(), (float)rng.NextDouble() * 2f - 1f);
                        rescued++;
                    }
                    entries[i] = new LevelLayoutData.PoseEntry { Position = pos, Rotation = t.rotation };
                }
                layout.Collectibles = entries;
                EditorUtility.SetDirty(layout);
                AssetDatabase.SaveAssets();
                Debug.Log("SortThem: baked " + count + " collectibles in " + steps + " steps, rescued " + rescued);
            }
            finally
            {
                Object.DestroyImmediate(root.gameObject);
                Physics.simulationMode = prevMode;
            }
        }

        const int SettleMaxSteps = 4000, SweepRounds = 6;
        const float RackTopY = 0.45f;

        static int Settle(List<GameObject> blockers, List<CarInstance> cars, int[] zones, GameManager gm, System.Random rng, float dt)
        {
            foreach (var go in blockers) if (go != null) Object.DestroyImmediate(go);
            blockers.Clear();
            Physics.SyncTransforms();
            foreach (var car in cars) { car.Body.isKinematic = false; car.Body.WakeUp(); }

            int steps = 0;
            var stranded = new List<int>();
            for (int round = 0; round <= SweepRounds; round++)
            {
                while (steps < SettleMaxSteps)
                {
                    Physics.Simulate(dt);
                    steps++;
                    if (steps % 25 == 0)
                    {
                        EditorUtility.DisplayProgressBar("Baking scatter", "settling on real geometry, round " + round + ", step " + steps, 0.5f + Mathf.Min(0.49f, steps / (float)SettleMaxSteps * 0.5f));
                        if (AllSleeping(cars)) break;
                    }
                }
                if (round == SweepRounds) break;
                stranded.Clear();
                for (int i = 0; i < cars.Count; i++) if (OnRack(cars[i])) stranded.Add(i);
                if (stranded.Count == 0) break;
                foreach (int i in stranded) PileDrop.Drop(cars[i], gm.Config, zones[i], rng);
                Physics.SyncTransforms();
            }
            EditorUtility.ClearProgressBar();
            return steps;
        }

        static bool OnRack(CarInstance car)
        {
            if (car.transform.position.y < RackTopY) return false;
            var hits = Physics.RaycastAll(car.transform.position, Vector3.down, car.HalfExtents.y + 0.08f, ~0, QueryTriggerInteraction.Ignore);
            foreach (var h in hits)
            {
                if (h.collider == car.Col) continue;
                if (h.collider.GetComponentInParent<RackController>() != null) return true;
            }
            return false;
        }

        static bool AllSleeping(List<CarInstance> cars)
        {
            foreach (var c in cars)
                if (!c.Body.IsSleeping()) return false;
            return true;
        }

        const float BlockHeight = 6f, SpecialBlockRadius = 3.2f, SpecialBlockHeight = 3.3f;

        static List<GameObject> BlockRacks(Transform root)
        {
            var made = new List<GameObject>();
            foreach (var rack in Object.FindObjectsByType<RackController>(FindObjectsSortMode.None))
            {
                if (rack.Zone == null) continue;
                var go = new GameObject("__RackBlock_" + rack.name);
                made.Add(go);
                go.transform.SetParent(root, false);
                var center = rack.Zone.transform.TransformPoint(rack.Zone.center);
                if (rack.Shelves != null && rack.Shelves.Length > 0 && rack.Shelves[0] != null && rack.Shelves[0].Locked)
                {
                    go.transform.position = new Vector3(center.x, 0f, center.z);
                    var cone = go.AddComponent<MeshCollider>();
                    cone.sharedMesh = ConeMesh(SpecialBlockRadius, SpecialBlockHeight, 24);
                    cone.convex = true;
                    cone.sharedMaterial = new PhysicsMaterial("__slippery") { dynamicFriction = 0f, staticFriction = 0f, frictionCombine = PhysicsMaterialCombine.Minimum, bounciness = 0f };
                    continue;
                }
                go.transform.SetPositionAndRotation(new Vector3(center.x, BlockHeight * 0.5f, center.z), rack.Zone.transform.rotation);
                var size = Vector3.Scale(rack.Zone.size, rack.Zone.transform.lossyScale);
                go.AddComponent<BoxCollider>().size = new Vector3(size.x, BlockHeight, size.z);
            }
            Physics.SyncTransforms();
            return made;
        }

        static Mesh ConeMesh(float radius, float height, int segments)
        {
            var verts = new Vector3[segments + 2];
            verts[0] = new Vector3(0f, height, 0f);
            verts[1] = Vector3.zero;
            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                verts[2 + i] = new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);
            }
            var tris = new int[segments * 6];
            for (int i = 0; i < segments; i++)
            {
                int a = 2 + i, b = 2 + (i + 1) % segments;
                tris[i * 6] = 0; tris[i * 6 + 1] = b; tris[i * 6 + 2] = a;
                tris[i * 6 + 3] = 1; tris[i * 6 + 4] = a; tris[i * 6 + 5] = b;
            }
            var mesh = new Mesh { vertices = verts, triangles = tris };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}

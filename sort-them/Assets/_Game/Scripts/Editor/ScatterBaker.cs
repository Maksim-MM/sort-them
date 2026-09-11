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
            if (gm == null || gm.Catalog == null || gm.Scatterer == null)
            {
                Debug.LogError("SortThem: open Main scene with GameManager first");
                return;
            }
            var catalog = gm.Catalog;
            var shelfData = AssetDatabase.LoadAssetAtPath<ShelfData>(Paths.Data + "/Shelf_Standard.asset");
            int perModel = copiesPerModel > 0 ? copiesPerModel : shelfData != null ? shelfData.Capacity : 10;

            var rng = new System.Random(gm.Scatterer.Seed);
            var order = new List<int>();
            for (int i = 0; i < catalog.Cars.Length; i++)
                for (int k = 0; k < perModel; k++) order.Add(i);
            for (int i = order.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }

            var prevMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;
            Physics.SyncTransforms();
            var root = new GameObject("__ScatterBake").transform;
            var spawned = new List<CarInstance>(order.Count);
            int total = order.Count;
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
                        gm.Scatterer.LaunchCar(car, rng);
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
                Debug.Log("SortThem: baked " + entries.Length + " cars in " + steps + " steps (" + (steps * dt).ToString("0.0") + " s simulated), rescued " + rescued + ", all sleeping=" + AllSleeping(spawned));
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
            if (gm == null || gm.Scatterer == null || layout == null || layout.CollectiblePrefab == null)
            {
                Debug.LogError("SortThem: need Main scene with GameManager, baked layout and canister prefab (menu 3e)");
                return;
            }
            int count = Mathf.Clamp(gm.Config.CollectiblesTotal, 1, 32);
            var rng = new System.Random(gm.Scatterer.Seed + 777);
            var prevMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;
            var root = new GameObject("__CollectibleBake").transform;
            var bodies = new List<Rigidbody>(count);
            try
            {
                var cars = new List<CarInstance>();
                CarSpawner.SpawnFromLayout(layout, root, cars);
                for (int i = 0; i < count; i++)
                {
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(layout.CollectiblePrefab, root);
                    var rb = go.GetComponent<Rigidbody>();
                    gm.Scatterer.LaunchBody(rb, rng, 1f);
                    float elevation = Mathf.Deg2Rad * (20f + (float)rng.NextDouble() * 20f);
                    float azimuth = (float)rng.NextDouble() * Mathf.PI * 2f;
                    float speed = 10f + (float)rng.NextDouble() * 5f;
                    rb.linearVelocity = new Vector3(Mathf.Cos(elevation) * Mathf.Cos(azimuth), Mathf.Sin(elevation), Mathf.Cos(elevation) * Mathf.Sin(azimuth)) * speed;
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

        static bool AllSleeping(List<CarInstance> cars)
        {
            foreach (var c in cars)
                if (!c.Body.IsSleeping()) return false;
            return true;
        }
    }
}

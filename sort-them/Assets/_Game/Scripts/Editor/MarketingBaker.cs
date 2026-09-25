using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SortThem.Editor
{
    public static class MarketingBaker
    {
        public const string Scene = Paths.Scenes + "/Marketing/Marketing.unity";
        public const string Layout = Paths.Scenes + "/Marketing/MarketingLayout.asset";

        const int MoundCars = 2000, CapCars = 500, SkirtCars = 800, HubCars = 600, WingCars = 2400;
        const float MoundRadius = 3.3f, CapRadius = 1.2f, SkirtMin = 1.0f, SkirtMax = 2.2f, HubMin = -12.6f, HubMax = -5.4f;
        const float MoundUprightFrom = 0.6f, WingUprightShare = 0.7f;
        const int FixRounds = 3;
        const float Dt = 0.02f, RayTop = 4.8f;
        static readonly Vector3 PodiumCenter = new Vector3(-9f, 0f, -9f);
        static readonly Rect[] WingAisles = { Rect.MinMaxRect(-12.6f, -4.3f, -10.4f, 12.9f), Rect.MinMaxRect(-7.6f, -4.3f, -5.4f, 12.9f) };

        [MenuItem("SortThem/Marketing/Bake Marketing Layout")]
        public static void Bake()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("SortThem: stop Play before baking");
                return;
            }
            var gm = Object.FindFirstObjectByType<GameManager>();
            var layout = AssetDatabase.LoadAssetAtPath<LevelLayoutData>(Layout);
            if (gm == null || !gm.DisableSave || gm.Layout != layout || layout == null)
            {
                Debug.LogError("SortThem: open " + Scene + " (GameManager with DisableSave and " + Layout + ")");
                return;
            }
            var catalog = gm.Catalog;
            var regular = new List<int>();
            var specials = new List<int>();
            for (int i = 0; i < catalog.Cars.Length; i++)
            {
                var c = catalog.Cars[i];
                if (c == null || c.Prefab == null) continue;
                (catalog.IsSpecial(c) ? specials : regular).Add(i);
            }

            var rng = new System.Random(gm.Scatterer != null ? gm.Scatterer.Seed : 12345);
            var prevMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;
            Physics.SyncTransforms();
            var root = new GameObject("__MarketingBake").transform;
            var cars = new List<CarInstance>();
            var order = new List<int>();
            var mound = new HashSet<CarInstance>();
            int steps = 0;
            try
            {
                var blockers = BlockRacks(root);

                for (int i = 0; i < MoundCars; i++)
                {
                    var car = Spawn(catalog, regular[rng.Next(regular.Count)], root, cars, order);
                    mound.Add(car);
                    Drop(car, MoundPoint(rng), i >= MoundCars * MoundUprightFrom, rng);
                    if (i % 3 == 2) steps += Step(1);
                    if (i % 150 == 0) Progress("mound " + i + "/" + MoundCars, i / (float)MoundCars * 0.4f);
                }
                for (int i = 0; i < SkirtCars; i++)
                {
                    var car = Spawn(catalog, regular[rng.Next(regular.Count)], root, cars, order);
                    mound.Add(car);
                    float a = Range(rng, 0f, Mathf.PI * 2f), r = Range(rng, SkirtMin, SkirtMax);
                    Drop(car, PodiumCenter + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r), rng.NextDouble() < WingUprightShare, rng);
                    if (i % 3 == 2) steps += Step(1);
                }
                for (int i = 0; i < CapCars; i++)
                {
                    var car = Spawn(catalog, regular[rng.Next(regular.Count)], root, cars, order);
                    mound.Add(car);
                    float a = Range(rng, 0f, Mathf.PI * 2f), r = CapRadius * Mathf.Sqrt((float)rng.NextDouble());
                    Drop(car, PodiumCenter + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r), true, rng);
                    if (i % 3 == 2) steps += Step(1);
                }
                for (int i = 0; i < HubCars; i++)
                {
                    var car = Spawn(catalog, regular[rng.Next(regular.Count)], root, cars, order);
                    Drop(car, new Vector3(Range(rng, HubMin, HubMax), 0f, Range(rng, HubMin, HubMax)), rng.NextDouble() < WingUprightShare, rng);
                    if (i % 3 == 2) steps += Step(1);
                }
                float wingArea = 0f;
                foreach (var r in WingAisles) wingArea += r.width * r.height;
                for (int i = 0; i < WingCars; i++)
                {
                    var car = Spawn(catalog, regular[rng.Next(regular.Count)], root, cars, order);
                    Drop(car, WingPoint(rng, wingArea), rng.NextDouble() < WingUprightShare, rng);
                    if (i % 3 == 2) steps += Step(1);
                    if (i % 150 == 0) Progress("wing " + i + "/" + WingCars, 0.4f + i / (float)WingCars * 0.2f);
                }
                steps += Settle(cars, 3000, "settle");

                foreach (var go in blockers) if (go != null) Object.DestroyImmediate(go);
                Physics.SyncTransforms();
                foreach (var c in cars) c.Body.WakeUp();
                steps += Settle(cars, 2000, "settle on racks");
                for (int round = 0; round < 3; round++)
                {
                    int stranded = 0;
                    foreach (var c in cars)
                    {
                        if (!OnRack(c)) continue;
                        stranded++;
                        var p = mound.Contains(c) ? MoundPoint(rng) : WingPoint(rng, wingArea);
                        Drop(c, p, true, rng);
                    }
                    if (stranded == 0) break;
                    steps += Settle(cars, 1500, "rescue " + stranded);
                }

                for (int round = 0; round < FixRounds; round++)
                {
                    int flipped = 0;
                    foreach (var c in mound)
                    {
                        if (c.transform.up.y >= 0.5f || !Exposed(c)) continue;
                        flipped++;
                        var p = c.transform.position;
                        Drop(c, new Vector3(p.x, 0f, p.z), true, rng);
                    }
                    Debug.Log("SortThem: marketing fix round " + round + ", flipped top cars " + flipped);
                    if (flipped == 0) break;
                    steps += Settle(cars, 1500, "upright round " + round);
                }

                foreach (int idx in specials)
                {
                    var car = Spawn(catalog, idx, root, cars, order);
                    float a = Range(rng, 0f, Mathf.PI * 2f), r = Range(rng, 0.2f, 0.7f);
                    Drop(car, PodiumCenter + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r), true, rng);
                }
                steps += Settle(cars, 1500, "specials");

                var entries = new List<LevelLayoutData.Entry>(cars.Count);
                int lost = 0;
                for (int i = 0; i < cars.Count; i++)
                {
                    var t = cars[i].transform;
                    if (t.position.y < gm.Config.FloorY - 0.2f) { lost++; continue; }
                    entries.Add(new LevelLayoutData.Entry { CarIndex = order[i], Position = t.position, Rotation = t.rotation });
                }
                int upTop = 0, top = 0;
                foreach (var c in mound) if (Exposed(c)) { top++; if (c.transform.up.y >= 0.5f) upTop++; }

                layout.Catalog = catalog;
                layout.Instances = entries.ToArray();
                layout.Collectibles = Array.Empty<LevelLayoutData.PoseEntry>();
                EditorUtility.SetDirty(layout);
                AssetDatabase.SaveAssets();
                Debug.Log("SortThem: marketing layout baked " + entries.Count + " cars, lost " + lost + ", steps " + steps + ", mound top upright " + upTop + "/" + top + ", all sleeping=" + AllSleeping(cars));
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                Object.DestroyImmediate(root.gameObject);
                Physics.simulationMode = prevMode;
            }
        }

        static CarInstance Spawn(CarCatalog catalog, int index, Transform root, List<CarInstance> cars, List<int> order)
        {
            var car = CarSpawner.Spawn(catalog.Cars[index], root, cars.Count);
            car.Col.sharedMaterial = BakeMaterial;
            cars.Add(car);
            order.Add(index);
            return car;
        }

        static PhysicsMaterial _bakeMaterial;
        static PhysicsMaterial BakeMaterial => _bakeMaterial != null ? _bakeMaterial : _bakeMaterial = new PhysicsMaterial("__marketingGrip") { staticFriction = 1.4f, dynamicFriction = 1.1f, frictionCombine = PhysicsMaterialCombine.Maximum, bounciness = 0f };

        static Vector3 MoundPoint(System.Random rng)
        {
            while (true)
            {
                float r = MoundRadius * Mathf.Sqrt((float)rng.NextDouble());
                if (rng.NextDouble() > 1f - r / MoundRadius) continue;
                float a = Range(rng, 0f, Mathf.PI * 2f);
                return PodiumCenter + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
            }
        }

        static Vector3 WingPoint(System.Random rng, float area)
        {
            float pick = Range(rng, 0f, area);
            foreach (var r in WingAisles)
            {
                pick -= r.width * r.height;
                if (pick <= 0f) return new Vector3(Range(rng, r.xMin, r.xMax), 0f, Range(rng, r.yMin, r.yMax));
            }
            var last = WingAisles[WingAisles.Length - 1];
            return new Vector3(last.center.x, 0f, last.center.y);
        }

        static void Drop(CarInstance car, Vector3 xz, bool upright, System.Random rng)
        {
            car.Col.enabled = false;
            float y = SurfaceY(xz) + (upright ? 0.25f : 0.5f);
            car.Col.enabled = true;
            var rot = upright
                ? Quaternion.Euler(Range(rng, -12f, 12f), Range(rng, 0f, 360f), Range(rng, -12f, 12f))
                : Quaternion.Euler(Range(rng, 0f, 360f), Range(rng, 0f, 360f), Range(rng, 0f, 360f));
            car.Launch(new Vector3(xz.x, y, xz.z), rot, upright ? Vector3.down * 0.5f : new Vector3(Range(rng, -0.5f, 0.5f), -Range(rng, 1f, 2f), Range(rng, -0.5f, 0.5f)));
            if (upright) car.Body.angularVelocity = Vector3.zero;
        }

        static float SurfaceY(Vector3 xz)
        {
            return Physics.Raycast(new Vector3(xz.x, RayTop, xz.z), Vector3.down, out var hit, 10f, ~0, QueryTriggerInteraction.Ignore) ? hit.point.y : 0f;
        }

        static bool Exposed(CarInstance car)
        {
            var p = car.transform.position;
            return Physics.Raycast(new Vector3(p.x, RayTop, p.z), Vector3.down, out var hit, 10f, ~0, QueryTriggerInteraction.Ignore) && hit.collider == car.Col;
        }

        static int Step(int count)
        {
            for (int i = 0; i < count; i++) Physics.Simulate(Dt);
            return count;
        }

        static int Settle(List<CarInstance> cars, int maxSteps, string label)
        {
            int steps = 0;
            while (steps < maxSteps)
            {
                Physics.Simulate(Dt);
                steps++;
                if (steps % 25 == 0)
                {
                    Progress(label + ", step " + steps, 0.6f + Mathf.Min(0.39f, steps / (float)maxSteps * 0.4f));
                    if (AllSleeping(cars)) break;
                }
            }
            return steps;
        }

        static void Progress(string info, float t) => EditorUtility.DisplayProgressBar("Baking marketing layout", info, t);

        static bool AllSleeping(List<CarInstance> cars)
        {
            foreach (var c in cars) if (!c.Body.isKinematic && !c.Body.IsSleeping()) return false;
            return true;
        }

        static bool OnRack(CarInstance car)
        {
            if (car.transform.position.y < 0.45f) return false;
            if ((new Vector2(car.transform.position.x, car.transform.position.z) - new Vector2(PodiumCenter.x, PodiumCenter.z)).magnitude < MoundRadius + 0.5f) return false;
            var hits = Physics.RaycastAll(car.transform.position, Vector3.down, car.HalfExtents.y + 0.08f, ~0, QueryTriggerInteraction.Ignore);
            foreach (var h in hits)
            {
                if (h.collider == car.Col) continue;
                if (h.collider.GetComponentInParent<RackController>() != null) return true;
            }
            return false;
        }

        static List<GameObject> BlockRacks(Transform root)
        {
            var made = new List<GameObject>();
            foreach (var rack in Object.FindObjectsByType<RackController>(FindObjectsSortMode.None))
            {
                if (rack.Zone == null) continue;
                if (rack.Shelves != null && rack.Shelves.Length > 0 && rack.Shelves[0] != null && rack.Shelves[0].Locked) continue;
                var go = new GameObject("__RackBlock_" + rack.name);
                go.transform.SetParent(root, false);
                var center = rack.Zone.transform.TransformPoint(rack.Zone.center);
                go.transform.SetPositionAndRotation(new Vector3(center.x, 3f, center.z), rack.Zone.transform.rotation);
                var size = Vector3.Scale(rack.Zone.size, rack.Zone.transform.lossyScale);
                go.AddComponent<BoxCollider>().size = new Vector3(size.x, 6f, size.z);
                made.Add(go);
            }
            Physics.SyncTransforms();
            return made;
        }

        static float Range(System.Random rng, float min, float max) => min + (float)rng.NextDouble() * (max - min);
    }
}

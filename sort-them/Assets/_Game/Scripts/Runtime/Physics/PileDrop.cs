using System.Collections.Generic;
using UnityEngine;

namespace SortThem
{
    public static class PileDrop
    {
        public static int ZoneCount(GameConfig cfg) => cfg.Piles != null && cfg.Piles.Length > 0 ? cfg.Piles.Length : 1;

        public static int[] Assign(GameConfig cfg, int count, System.Random rng)
        {
            var result = new int[count];
            int zones = ZoneCount(cfg);
            if (zones == 1 || count == 0) return result;
            float total = 0f;
            foreach (var z in cfg.Piles) total += Mathf.Max(0f, z.Share);
            if (total <= 0f) return result;

            var quota = new int[zones];
            int assigned = 0;
            for (int z = 0; z < zones; z++)
            {
                quota[z] = Mathf.FloorToInt(count * Mathf.Max(0f, cfg.Piles[z].Share) / total);
                assigned += quota[z];
            }
            int rest = count - assigned;
            for (int z = 0, guard = 0; rest > 0 && guard < zones * 4; z = (z + 1) % zones, guard++)
                if (cfg.Piles[z].Share > 0f) { quota[z]++; rest--; }

            var keys = new List<(float key, int zone)>(count);
            for (int z = 0; z < zones; z++)
                for (int k = 0; k < quota[z]; k++) keys.Add(((k + 0.5f) / quota[z], z));
            keys.Sort((a, b) => a.key.CompareTo(b.key));
            for (int i = 0; i < count && i < keys.Count; i++) result[i] = keys[i].zone;
            return result;
        }

        public static int PickZone(GameConfig cfg, System.Random rng)
        {
            int zones = ZoneCount(cfg);
            if (zones == 1) return 0;
            float total = 0f;
            foreach (var z in cfg.Piles) total += Mathf.Max(0f, z.Share);
            float r = (float)rng.NextDouble() * total;
            for (int z = 0; z < zones; z++)
            {
                r -= Mathf.Max(0f, cfg.Piles[z].Share);
                if (r <= 0f) return z;
            }
            return zones - 1;
        }

        public static Vector3 Point(GameConfig cfg, int zone, System.Random rng)
        {
            Vector3 center;
            Vector2 half;
            if (cfg.Piles == null || cfg.Piles.Length == 0)
            {
                center = cfg.UnstuckCenter;
                half = new Vector2(1.5f, 1.5f);
            }
            else
            {
                var z = cfg.Piles[Mathf.Clamp(zone, 0, cfg.Piles.Length - 1)];
                center = z.Center;
                half = z.HalfSize;
            }
            return new Vector3(center.x + Range(rng, -half.x, half.x), cfg.FloorY + cfg.PileDropHeight + Range(rng, 0f, 0.6f), center.z + Range(rng, -half.y, half.y));
        }

        public static void Drop(CarInstance car, GameConfig cfg, int zone, System.Random rng)
        {
            car.Launch(Point(cfg, zone, rng), RandomRotation(rng), RandomVelocity(rng));
        }

        public static void DropBody(Rigidbody body, GameConfig cfg, int zone, System.Random rng)
        {
            var pos = Point(cfg, zone, rng);
            var rot = RandomRotation(rng);
            body.isKinematic = false;
            body.position = pos;
            body.rotation = rot;
            body.transform.SetPositionAndRotation(pos, rot);
            body.linearVelocity = RandomVelocity(rng);
            body.angularVelocity = new Vector3(Range(rng, -3f, 3f), Range(rng, -3f, 3f), Range(rng, -3f, 3f));
        }

        static Quaternion RandomRotation(System.Random rng) => Quaternion.Euler(Range(rng, 0f, 360f), Range(rng, 0f, 360f), Range(rng, 0f, 360f));

        static Vector3 RandomVelocity(System.Random rng) => new Vector3(Range(rng, -0.5f, 0.5f), -Range(rng, 1f, 3f), Range(rng, -0.5f, 0.5f));

        static float Range(System.Random rng, float min, float max) => min + (float)rng.NextDouble() * (max - min);
    }
}

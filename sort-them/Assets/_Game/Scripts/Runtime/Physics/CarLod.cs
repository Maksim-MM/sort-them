using System.Collections.Generic;
using UnityEngine;

namespace SortThem
{
    public static class CarLod
    {
        static readonly float[] _t2 = new float[8];

        public static void Tick(List<CarInstance> cars, Vector3 eye, float[] distances, int slices = 1, int frame = 0)
        {
            if (distances == null || distances.Length == 0) return;
            int levels = Mathf.Min(distances.Length, _t2.Length);
            for (int i = 0; i < levels; i++) _t2[i] = distances[i] * distances[i];
            if (slices < 1) slices = 1;
            for (int i = frame % slices; i < cars.Count; i += slices)
            {
                var car = cars[i];
                var lods = car.Lods;
                if (lods == null || lods.Length < 2 || car.State == CarState.Held) continue;
                int max = Mathf.Min(lods.Length, levels + 1) - 1;
                float d2 = (car.transform.position - eye).sqrMagnitude;
                int lvl = 0;
                while (lvl < max && d2 > _t2[lvl]) lvl++;
                int cur = car.Lod;
                if (lvl == cur) continue;
                if (lvl > cur && d2 < _t2[cur] * 1.21f) continue;
                if (lvl < cur && d2 > _t2[lvl] * 0.83f) continue;
                var mesh = lods[lvl];
                if (mesh == null) continue;
                car.Lod = lvl;
                var f = car.Filter;
                if (f != null && f.sharedMesh != mesh) f.sharedMesh = mesh;
            }
        }
    }
}

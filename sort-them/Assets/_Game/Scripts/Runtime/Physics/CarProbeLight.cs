using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SortThem
{
    public static class CarProbeLight
    {
        const float MoveThreshold2 = 0.05f * 0.05f;
        static readonly int ProbeR = Shader.PropertyToID("_ProbeR");
        static readonly int ProbeG = Shader.PropertyToID("_ProbeG");
        static readonly int ProbeB = Shader.PropertyToID("_ProbeB");
        static MaterialPropertyBlock _block;

        public static void ApplyAll(List<CarInstance> cars)
        {
            for (int i = 0; i < cars.Count; i++) Apply(cars[i]);
        }

        public static void Tick(List<CarInstance> cars, int slices, int frame)
        {
            if (slices < 1) slices = 1;
            for (int i = frame % slices; i < cars.Count; i += slices)
            {
                var car = cars[i];
                if (car == null || car.State == CarState.Held) continue;
                if ((car.transform.position - car.LitPosition).sqrMagnitude > MoveThreshold2) Apply(car);
            }
        }

        public static void Apply(CarInstance car)
        {
            if (car == null) return;
            var r = car.Rend;
            if (r == null) return;
            var pos = car.transform.position;
            car.LitPosition = pos;
            LightProbes.GetInterpolatedProbe(r.bounds.center, r.lightProbeUsage == LightProbeUsage.Off ? null : r, out SphericalHarmonicsL2 sh);
            _block ??= new MaterialPropertyBlock();
            _block.Clear();
            _block.SetVector(ProbeR, new Vector4(sh[0, 3], sh[0, 1], sh[0, 2], sh[0, 0]));
            _block.SetVector(ProbeG, new Vector4(sh[1, 3], sh[1, 1], sh[1, 2], sh[1, 0]));
            _block.SetVector(ProbeB, new Vector4(sh[2, 3], sh[2, 1], sh[2, 2], sh[2, 0]));
            r.SetPropertyBlock(_block);
            if (r.lightProbeUsage != LightProbeUsage.Off) r.lightProbeUsage = LightProbeUsage.Off;
        }
    }
}

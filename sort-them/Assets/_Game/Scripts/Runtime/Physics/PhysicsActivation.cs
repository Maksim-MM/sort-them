using System.Collections.Generic;
using UnityEngine;

namespace SortThem
{
    public static class PhysicsActivation
    {
        public static void Tick(List<CarInstance> cars, Vector3 center, float radius, float freezeSpeed, float freezeDelay, int slices = 1, int frame = 0)
        {
            float r2 = radius * radius;
            float fs2 = freezeSpeed * freezeSpeed;
            float now = Time.time;
            if (slices < 1) slices = 1;
            for (int i = frame % slices; i < cars.Count; i += slices)
            {
                var car = cars[i];
                if (car.State != CarState.Loose || car.Levitating) continue;
                bool inside = (car.transform.position - center).sqrMagnitude <= r2;
                if (inside)
                {
                    car.CalmSince = -1f;
                    if (car.Body.isKinematic) car.Unfreeze();
                    continue;
                }
                var body = car.Body;
                if (body.isKinematic) continue;
                if (body.IsSleeping())
                {
                    car.Freeze();
                    car.CalmSince = -1f;
                }
                else if (body.linearVelocity.sqrMagnitude < fs2 && body.angularVelocity.sqrMagnitude < 4f)
                {
                    if (car.CalmSince < 0f) car.CalmSince = now;
                    else if (now - car.CalmSince >= freezeDelay)
                    {
                        car.Freeze();
                        car.CalmSince = -1f;
                    }
                }
                else car.CalmSince = -1f;
            }
        }
    }
}

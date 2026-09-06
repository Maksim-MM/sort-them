using System.Collections.Generic;
using UnityEngine;

namespace SortThem
{
    public static class PhysicsActivation
    {
        public static void Tick(List<CarInstance> cars, Vector3 center, float radius)
        {
            float r2 = radius * radius;
            for (int i = 0; i < cars.Count; i++)
            {
                var car = cars[i];
                if (car.State != CarState.Loose || car.Levitating) continue;
                bool inside = (car.transform.position - center).sqrMagnitude <= r2;
                if (inside)
                {
                    if (car.Body.isKinematic) car.Unfreeze();
                }
                else if (!car.Body.isKinematic && car.Body.IsSleeping())
                {
                    car.Freeze();
                }
            }
        }
    }
}

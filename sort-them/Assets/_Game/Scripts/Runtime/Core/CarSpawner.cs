using System.Collections.Generic;
using UnityEngine;

namespace SortThem
{
    public static class CarSpawner
    {
        public static void SpawnFromLayout(LevelLayoutData layout, Transform root, List<CarInstance> into)
        {
            into.Clear();
            if (layout == null || layout.Catalog == null)
            {
                Debug.LogWarning("SortThem: no level layout, nothing spawned");
                return;
            }
            var cars = layout.Catalog.Cars;
            for (int i = 0; i < layout.Instances.Length; i++)
            {
                var e = layout.Instances[i];
                if (e.CarIndex < 0 || e.CarIndex >= cars.Length || cars[e.CarIndex] == null || cars[e.CarIndex].Prefab == null) continue;
                var car = Spawn(cars[e.CarIndex], root, i);
                car.SetLoose(e.Position, e.Rotation, true);
                into.Add(car);
            }
        }

        public static CarInstance Spawn(CarItemData data, Transform root, int instanceId)
        {
            var go = Object.Instantiate(data.Prefab, root);
            go.name = data.DevName + "_" + instanceId;
            var car = go.GetComponent<CarInstance>();
            if (car == null) car = go.AddComponent<CarInstance>();
            car.InstanceId = instanceId;
            car.Data = data;
            return car;
        }
    }
}

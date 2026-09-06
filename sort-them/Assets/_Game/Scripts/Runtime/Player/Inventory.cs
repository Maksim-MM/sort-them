using System;
using System.Collections.Generic;
using UnityEngine;

namespace SortThem
{
    public class Inventory : MonoBehaviour
    {
        public readonly List<CarInstance> Items = new List<CarInstance>();
        public int ActiveIndex;
        public CarInstance LastAdded { get; private set; }
        public Vector3 LastAddedPosition { get; private set; }
        public Quaternion LastAddedRotation { get; private set; }
        public int AddCount { get; private set; }
        public int RemoveCount { get; private set; }

        public event Action Changed;

        public int Capacity
        {
            get
            {
                var gm = GameManager.I;
                if (gm == null) return 5;
                return gm.Config.BaseInventoryCapacity + Mathf.RoundToInt(gm.Upgrades.Value(UpgradeKind.Inventory, 0f));
            }
        }

        public bool IsFull => Items.Count >= Capacity;
        public CarInstance Active => Items.Count > 0 ? Items[Mathf.Clamp(ActiveIndex, 0, Items.Count - 1)] : null;

        public bool Add(CarInstance car)
        {
            if (car == null || IsFull) return false;
            LastAdded = car;
            LastAddedPosition = car.transform.position;
            LastAddedRotation = car.transform.rotation;
            AddCount++;
            car.SetHeld();
            Items.Add(car);
            ActiveIndex = Items.Count - 1;
            Changed?.Invoke();
            return true;
        }

        public CarInstance RemoveActive()
        {
            var car = Active;
            if (car == null) return null;
            Items.RemoveAt(Mathf.Clamp(ActiveIndex, 0, Items.Count - 1));
            if (ActiveIndex >= Items.Count) ActiveIndex = Mathf.Max(0, Items.Count - 1);
            RemoveCount++;
            Changed?.Invoke();
            return car;
        }

        public void Next()
        {
            if (Items.Count == 0) return;
            ActiveIndex = (ActiveIndex + 1) % Items.Count;
            Changed?.Invoke();
        }

        public void Prev()
        {
            if (Items.Count == 0) return;
            ActiveIndex = (ActiveIndex - 1 + Items.Count) % Items.Count;
            Changed?.Invoke();
        }

        public void NotifyChanged() => Changed?.Invoke();
    }
}

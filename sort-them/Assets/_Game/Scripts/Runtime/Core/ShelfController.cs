using System;
using System.Collections;
using UnityEngine;

namespace SortThem
{
    public class ShelfController : MonoBehaviour
    {
        public int ShelfId;
        public ShelfData Data;
        public RackController Rack;
        public Transform[] SlotPoints = Array.Empty<Transform>();
        public PriceTag Tag;
        public GameObject HighlightMarker;
        public Collider Zone;

        public CarItemData TargetCar;
        public int Count { get; private set; }

        CarInstance[] _slots;

        public event Action<ShelfController> Changed;

        public int Capacity => SlotPoints.Length;
        public bool IsEmpty => TargetCar == null;
        public bool IsValid => TargetCar != null && Rack != null && Rack.Category != null && TargetCar.Category == Rack.Category;
        public bool IsComplete => TargetCar != null && Count >= Capacity;
        public bool IsClosed => IsComplete && IsValid;

        void Awake()
        {
            EnsureSlots();
        }

        void EnsureSlots()
        {
            if (_slots == null || _slots.Length != Capacity) _slots = new CarInstance[Capacity];
        }

        public bool Accepts(CarItemData car)
        {
            return car != null && (TargetCar == null || TargetCar == car) && Count < Capacity;
        }

        public CarInstance GetSlot(int index)
        {
            EnsureSlots();
            return index >= 0 && index < _slots.Length ? _slots[index] : null;
        }

        public int FirstFreeSlot()
        {
            EnsureSlots();
            for (int i = 0; i < _slots.Length; i++)
                if (_slots[i] == null) return i;
            return -1;
        }

        public CarInstance LastPlaced()
        {
            EnsureSlots();
            for (int i = _slots.Length - 1; i >= 0; i--)
                if (_slots[i] != null) return _slots[i];
            return null;
        }

        public int NearestFreeSlot(Vector3 worldPoint)
        {
            EnsureSlots();
            int best = -1;
            float bestDist = float.MaxValue;
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] != null) continue;
                float d = (SlotPoints[i].position - worldPoint).sqrMagnitude;
                if (d < bestDist)
                {
                    bestDist = d;
                    best = i;
                }
            }
            return best;
        }

        public bool TryPlace(CarInstance car, int slot, bool pay = true)
        {
            EnsureSlots();
            if (car == null || !Accepts(car.Data)) return false;
            if (slot < 0 || slot >= Capacity || _slots[slot] != null) return false;

            if (TargetCar == null) TargetCar = car.Data;
            _slots[slot] = car;
            Count++;
            car.SetPlaced(this, slot, SlotPoints[slot]);

            var gm = GameManager.I;
            float reward = gm != null ? gm.EconomyConfig.RewardPerCar : 1f;
            float paid = IsValid ? reward : 0f;
            car.PaidAmount = paid;
            if (pay && gm != null)
            {
                if (paid != 0f) gm.Economy.Add(paid);
                if (IsClosed)
                {
                    if (gm.EconomyConfig.ShelfCompleteBonus != 0f) gm.Economy.Add(gm.EconomyConfig.ShelfCompleteBonus);
                    gm.OnShelfClosed(this);
                }
            }

            Changed?.Invoke(this);
            if (gm != null) gm.OnShelfChanged(this);
            return true;
        }

        public void Remove(CarInstance car)
        {
            EnsureSlots();
            if (car == null || car.Shelf != this || car.SlotIndex < 0 || car.SlotIndex >= _slots.Length) return;
            if (_slots[car.SlotIndex] != car) return;

            _slots[car.SlotIndex] = null;
            Count--;
            var gm = GameManager.I;
            if (car.PaidAmount != 0f && gm != null) gm.Economy.Add(-car.PaidAmount);
            car.PaidAmount = 0f;
            car.Shelf = null;
            car.SlotIndex = -1;
            if (Count <= 0)
            {
                Count = 0;
                TargetCar = null;
            }

            Changed?.Invoke(this);
            if (gm != null) gm.OnShelfChanged(this);
        }

        public void OnLooseCarEntered(CarInstance car)
        {
            if (car == null || car.State != CarState.Loose || car.Body.isKinematic) return;
            var gm = GameManager.I;
            if (gm == null) return;

            if (gm.Upgrades.Has(UpgradeKind.AutoPlace) && Rack != null && Rack.Category != null && car.Data != null && car.Data.Category == Rack.Category)
            {
                var target = Rack.FindAutoPlaceShelf(car.Data);
                int slot = target != null ? target.FirstFreeSlot() : -1;
                if (slot >= 0)
                {
                    target.StartCoroutine(target.Magnet(car, slot));
                    return;
                }
            }

            if (TargetCar != null && (TargetCar != car.Data || Count >= Capacity))
                Bounce(car);
        }

        public IEnumerator Magnet(CarInstance car, int slot)
        {
            Vector3 fromPos = car.transform.position;
            Quaternion fromRot = car.transform.rotation;
            if (!TryPlace(car, slot)) yield break;
            Vector3 toPos = car.transform.position;
            Quaternion toRot = car.transform.rotation;
            float duration = GameManager.I != null ? GameManager.I.Config.MagnetDuration : 0.25f;
            float t = 0f;
            while (t < duration && car.State == CarState.Placed && car.Shelf == this)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t / duration);
                car.transform.SetPositionAndRotation(Vector3.Lerp(fromPos, toPos, k), Quaternion.Slerp(fromRot, toRot, k));
                yield return null;
            }
            if (car.State == CarState.Placed && car.Shelf == this)
                car.transform.SetPositionAndRotation(toPos, toRot);
        }

        void Bounce(CarInstance car)
        {
            float speed = GameManager.I != null ? GameManager.I.Config.BounceSpeed : 3f;
            Vector3 dir = (transform.forward + Vector3.up * 0.7f).normalized;
            car.Body.linearVelocity = dir * speed;
        }

        public void SetHighlight(bool on)
        {
            if (HighlightMarker != null && HighlightMarker.activeSelf != on) HighlightMarker.SetActive(on);
        }
    }
}

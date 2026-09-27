using System.Collections.Generic;
using UnityEngine;

namespace SortThem
{
    public class RackZone : MonoBehaviour
    {
        public RackController Rack;

        readonly HashSet<CarInstance> _inside = new HashSet<CarInstance>();
        static readonly List<CarInstance> _scan = new List<CarInstance>();
        Collider _zone;

        void Awake() => _zone = GetComponent<Collider>();

        void OnDisable() => _inside.Clear();

        void OnTriggerEnter(Collider other)
        {
            var car = CarOf(other);
            if (car != null) _inside.Add(car);
        }

        void OnTriggerExit(Collider other)
        {
            var car = CarOf(other);
            if (car != null) _inside.Remove(car);
        }

        void FixedUpdate()
        {
            if (Rack == null || _inside.Count == 0) return;
            var gm = GameManager.I;
            if (gm == null || !gm.Upgrades.Has(UpgradeKind.AutoPlace)) return;
            _scan.Clear();
            _scan.AddRange(_inside);
            foreach (var car in _scan)
            {
                if (car == null || !car.gameObject.activeInHierarchy) { _inside.Remove(car); continue; }
                if (car.State != CarState.Loose || car.Levitating || car.Body.isKinematic) continue;
                if (car.Body.linearVelocity.sqrMagnitude < 0.25f) continue;
                if (!Overlaps(car)) { _inside.Remove(car); continue; }
                Rack.OnLooseCarInside(car);
            }
        }

        bool Overlaps(CarInstance car)
        {
            var col = car.Col;
            if (_zone == null || col == null || !col.enabled) return false;
            var zt = _zone.transform;
            var ct = col.transform;
            return Physics.ComputePenetration(_zone, zt.position, zt.rotation, col, ct.position, ct.rotation, out _, out _);
        }

        static CarInstance CarOf(Collider other)
        {
            var body = other.attachedRigidbody;
            return body != null ? body.GetComponent<CarInstance>() : null;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace SortThem
{
    public static class PileOcclusion
    {
        public static bool Ready { get; private set; }
        public static int HiddenCount { get; private set; }
        public static int Checked { get; private set; }

        static readonly HashSet<CarInstance> _dirty = new HashSet<CarInstance>();
        static readonly List<CarInstance> _batch = new List<CarInstance>();
        static readonly Dictionary<Collider, CarInstance> _byCollider = new Dictionary<Collider, CarInstance>();
        static readonly Collider[] _overlap = new Collider[64];
        static readonly RaycastHit[] _hits = new RaycastHit[4];
        static readonly Vector3[] Dirs =
        {
            Vector3.up,
            new Vector3(1f, 1f, 1f).normalized, new Vector3(-1f, 1f, 1f).normalized,
            new Vector3(1f, 1f, -1f).normalized, new Vector3(-1f, 1f, -1f).normalized,
            Vector3.forward, Vector3.back, Vector3.left, Vector3.right,
            new Vector3(1f, 0f, 1f).normalized, new Vector3(-1f, 0f, 1f).normalized,
            new Vector3(1f, 0f, -1f).normalized, new Vector3(-1f, 0f, -1f).normalized
        };
        static GameConfig _cfg;
        static int _mask;

        public static void Init(List<CarInstance> cars, GameConfig cfg)
        {
            _cfg = cfg;
            _mask = 1 << Layers.LooseItems;
            _dirty.Clear();
            _byCollider.Clear();
            HiddenCount = 0;
            foreach (var car in cars) _byCollider[car.Col] = car;
            Ready = true;
            if (!cfg.BuriedCulling) { ShowAll(cars); return; }
            Physics.SyncTransforms();
            foreach (var car in cars) Evaluate(car);
        }

        public static void Shutdown()
        {
            Ready = false;
            _dirty.Clear();
            _byCollider.Clear();
            HiddenCount = 0;
        }

        public static void SetEnabled(List<CarInstance> cars, bool on)
        {
            if (!Ready) return;
            _cfg.BuriedCulling = on;
            if (on) foreach (var car in cars) Evaluate(car);
            else ShowAll(cars);
        }

        static void ShowAll(List<CarInstance> cars)
        {
            foreach (var car in cars) car.SetVisible(true);
            HiddenCount = 0;
        }

        public static void MarkDirty(CarInstance car)
        {
            if (!Ready || !_cfg.BuriedCulling || car == null) return;
            _dirty.Add(car);
            int n = Physics.OverlapSphereNonAlloc(car.transform.position, _cfg.BuriedNeighborRadius, _overlap, _mask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
                if (_byCollider.TryGetValue(_overlap[i], out var other)) _dirty.Add(other);
        }

        public static void Tick()
        {
            if (!Ready || !_cfg.BuriedCulling || _dirty.Count == 0) return;
            _batch.Clear();
            foreach (var car in _dirty) { _batch.Add(car); if (_batch.Count >= _cfg.BuriedPerFrame) break; }
            foreach (var car in _batch) { _dirty.Remove(car); Evaluate(car); }
        }

        static void Evaluate(CarInstance car)
        {
            if (car == null) return;
            bool hide = car.State == CarState.Loose && !car.Levitating && car.Body.isKinematic && car.gameObject.activeSelf && IsBuried(car);
            bool wasHidden = car.Hidden;
            car.SetVisible(!hide);
            if (hide && !wasHidden) HiddenCount++;
            else if (!hide && wasHidden) HiddenCount--;
        }

        static bool IsBuried(CarInstance car)
        {
            Checked++;
            var origin = car.Col.bounds.center;
            float len = _cfg.BuriedRayLength;
            int count = _cfg.BuriedSideRays ? Dirs.Length : 5;
            for (int d = 0; d < count; d++)
            {
                int n = Physics.RaycastNonAlloc(origin, Dirs[d], _hits, len, _mask, QueryTriggerInteraction.Ignore);
                bool covered = false;
                for (int i = 0; i < n; i++) if (_hits[i].collider != car.Col) { covered = true; break; }
                if (!covered) return false;
            }
            return true;
        }
    }
}

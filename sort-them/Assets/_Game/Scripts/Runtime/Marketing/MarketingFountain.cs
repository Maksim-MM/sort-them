#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SortThem
{
    public class MarketingFountain : MonoBehaviour
    {
        public Vector3 Source = new Vector3(-9f, 1.7f, -9f);
        public float CarsPerSecond = 40f;
        public float MinSpeed = 5f;
        public float MaxSpeed = 7.5f;
        public float ConeHalfAngle = 35f;
        public float SpawnJitter = 0.25f;
        public float Spin = 4f;
        public int MaxCars;
        public bool HideLooseOnStart = true;

        readonly List<CarInstance> _queue = new List<CarInstance>();
        int _next;
        float _budget;
        bool _prepared;

        public bool Running { get; private set; }

        void Update()
        {
            var gm = GameManager.I;
            if (gm == null || !gm.Ready) return;
            if (!_prepared) Prepare(gm);

            var pad = Gamepad.current;
            if (pad != null && pad.leftShoulder.isPressed && pad.rightShoulder.isPressed && (pad.leftShoulder.wasPressedThisFrame || pad.rightShoulder.wasPressedThisFrame)) Toggle();

            if (!Running) return;
            _budget += CarsPerSecond * Time.deltaTime;
            int limit = MaxCars > 0 ? Mathf.Min(MaxCars, _queue.Count) : _queue.Count;
            while (_budget >= 1f && _next < limit)
            {
                _budget -= 1f;
                Launch(_queue[_next++]);
            }
            if (_next >= limit) Running = false;
        }

        public void Toggle()
        {
            if (!_prepared) return;
            Running = !Running;
            _budget = 0f;
        }

        void Prepare(GameManager gm)
        {
            _prepared = true;
            _queue.Clear();
            foreach (var car in gm.Cars)
                if (car.State == CarState.Loose) _queue.Add(car);
            for (int i = _queue.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (_queue[i], _queue[j]) = (_queue[j], _queue[i]);
            }
            if (HideLooseOnStart)
                foreach (var car in _queue) car.gameObject.SetActive(false);
        }

        void Launch(CarInstance car)
        {
            float angle = Random.Range(0f, ConeHalfAngle) * Mathf.Deg2Rad;
            float azimuth = Random.Range(0f, Mathf.PI * 2f);
            var dir = new Vector3(Mathf.Sin(angle) * Mathf.Cos(azimuth), Mathf.Cos(angle), Mathf.Sin(angle) * Mathf.Sin(azimuth));
            var pos = Source + new Vector3(Random.Range(-SpawnJitter, SpawnJitter), Random.Range(0f, SpawnJitter), Random.Range(-SpawnJitter, SpawnJitter));
            car.Launch(pos, Random.rotation, dir * Random.Range(MinSpeed, MaxSpeed));
            car.Body.angularVelocity = Random.insideUnitSphere * Spin;
            car.CalmSince = -1f;
        }
    }
}
#endif

using UnityEngine;

namespace SortThem
{
    public class UiPulse : MonoBehaviour
    {
        public float Scale = 1.12f;
        public float Speed = 4f;

        Vector3 _base;

        void OnEnable() { _base = transform.localScale; }
        void OnDisable() { transform.localScale = _base; }

        void Update()
        {
            float k = 1f + (Scale - 1f) * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * Speed));
            transform.localScale = _base * k;
        }
    }
}

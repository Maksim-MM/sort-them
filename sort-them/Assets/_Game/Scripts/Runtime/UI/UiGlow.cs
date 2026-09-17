using UnityEngine;
using UnityEngine.UI;

namespace SortThem
{
    public class UiGlow : MonoBehaviour
    {
        public Image Source;
        public RectTransform Target;
        public float Spread = 16f;
        public float Alpha = 0.55f;
        public float Flicker = 0.10f;

        Image _img;
        RectTransform _rt;
        float _seed;

        void Awake()
        {
            _img = GetComponent<Image>();
            _rt = (RectTransform)transform;
            _seed = Random.value * 100f;
        }

        void LateUpdate()
        {
            if (Target != null) Follow();
            var c = Source != null ? Source.color : _img.color;
            float n = Flicker > 0f ? 1f - Flicker * Mathf.PerlinNoise(Time.unscaledTime * 6f, _seed) : 1f;
            c.a = Alpha * n;
            if (_img.color != c) _img.color = c;
            bool show = Target == null || Target.gameObject.activeSelf;
            if (_img.enabled != show) _img.enabled = show;
        }

        public void Follow()
        {
            _rt.anchorMin = Target.anchorMin;
            _rt.anchorMax = Target.anchorMax;
            _rt.pivot = Target.pivot;
            _rt.anchoredPosition = Target.anchoredPosition + Spread * (2f * Target.pivot - Vector2.one);
            _rt.sizeDelta = Target.sizeDelta + new Vector2(Spread * 2f, Spread * 2f);
        }
    }
}

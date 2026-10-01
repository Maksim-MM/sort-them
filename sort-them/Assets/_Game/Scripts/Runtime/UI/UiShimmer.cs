using UnityEngine;
using UnityEngine.UI;

namespace SortThem
{
    public class UiShimmer : MonoBehaviour
    {
        public float Speed = 420f;
        public float Length = 90f;
        public float Thickness = 4f;
        public Color Tint = new Color(1f, 0.95f, 0.6f, 1f);

        static Texture2D _tex;
        RectTransform _rt, _seg;
        float _t;

        void Awake()
        {
            _rt = (RectTransform)transform;
            var go = new GameObject("Shimmer", typeof(RectTransform));
            go.transform.SetParent(_rt, false);
            _seg = (RectTransform)go.transform;
            _seg.anchorMin = _seg.anchorMax = Vector2.zero;
            _seg.pivot = new Vector2(0.5f, 0.5f);
            var img = go.AddComponent<RawImage>();
            img.texture = Tex();
            img.color = Tint;
            img.raycastTarget = false;
        }

        static Texture2D Tex()
        {
            if (_tex != null) return _tex;
            const int n = 64;
            _tex = new Texture2D(n, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[n];
            for (int i = 0; i < n; i++)
            {
                float a = Mathf.Sin((i + 0.5f) / n * Mathf.PI);
                px[i] = new Color32(255, 255, 255, (byte)(a * a * 255f));
            }
            _tex.SetPixels32(px);
            _tex.Apply();
            return _tex;
        }

        void Update()
        {
            var r = _rt.rect;
            float w = r.width, h = r.height;
            if (w <= 0f || h <= 0f) return;
            float per = 2f * (w + h);
            _t = (_t + Speed * Time.unscaledDeltaTime) % per;
            float d = _t, half = Thickness * 0.5f;
            Vector2 pos;
            float rot;
            if (d < w) { pos = new Vector2(d, h - half); rot = 0f; }
            else if (d < w + h) { pos = new Vector2(w - half, h - (d - w)); rot = 90f; }
            else if (d < 2f * w + h) { pos = new Vector2(w - (d - w - h), half); rot = 0f; }
            else { pos = new Vector2(half, d - 2f * w - h); rot = 90f; }
            _seg.anchoredPosition = pos;
            _seg.sizeDelta = new Vector2(Mathf.Min(Length, Mathf.Min(w, h)), Thickness);
            _seg.localRotation = Quaternion.Euler(0f, 0f, rot);
        }
    }
}

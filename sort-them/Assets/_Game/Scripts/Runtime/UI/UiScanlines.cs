using UnityEngine;
using UnityEngine.UI;

namespace SortThem
{
    [RequireComponent(typeof(RawImage))]
    public class UiScanlines : MonoBehaviour
    {
        public float Period = 3f;

        RawImage _raw;
        RectTransform _rt;

        void Awake()
        {
            _raw = GetComponent<RawImage>();
            _rt = (RectTransform)transform;
        }

        void OnEnable() { Refresh(); }
        void OnRectTransformDimensionsChange() { Refresh(); }

        void Refresh()
        {
            if (_raw == null || _rt == null) return;
            float h = _rt.rect.height;
            if (h <= 0f) return;
            _raw.uvRect = new Rect(0f, 0f, 1f, h / Period);
        }
    }
}

using UnityEngine;

namespace SortThem
{
    public class SafeAreaFitter : MonoBehaviour
    {
        Rect _applied = new Rect(-1f, -1f, 0f, 0f);
        RectTransform _rect;

        void Awake() => _rect = GetComponent<RectTransform>();

        void Update()
        {
            var safe = Screen.safeArea;
            if (safe == _applied) return;
            _applied = safe;
            float w = Mathf.Max(1f, Screen.width), h = Mathf.Max(1f, Screen.height);
            _rect.anchorMin = new Vector2(safe.xMin / w, safe.yMin / h);
            _rect.anchorMax = new Vector2(safe.xMax / w, safe.yMax / h);
            _rect.offsetMin = Vector2.zero;
            _rect.offsetMax = Vector2.zero;
        }
    }
}

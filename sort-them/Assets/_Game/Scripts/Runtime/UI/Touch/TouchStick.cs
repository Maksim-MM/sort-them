using UnityEngine;
using UnityEngine.EventSystems;

namespace SortThem
{
    public class TouchStick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public RectTransform Knob;
        public float Radius = 80f;

        RectTransform _rect;

        void Awake() => _rect = GetComponent<RectTransform>();

        public void OnPointerDown(PointerEventData e) => Apply(e);
        public void OnDrag(PointerEventData e) => Apply(e);

        public void OnPointerUp(PointerEventData e)
        {
            TouchInput.Move = Vector2.zero;
            if (Knob != null) Knob.anchoredPosition = Vector2.zero;
        }

        void OnDisable()
        {
            TouchInput.Move = Vector2.zero;
            if (Knob != null) Knob.anchoredPosition = Vector2.zero;
        }

        void Apply(PointerEventData e)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_rect, e.position, e.pressEventCamera, out var local)) return;
            var v = Vector2.ClampMagnitude(local / Radius, 1f);
            TouchInput.Move = v;
            if (Knob != null) Knob.anchoredPosition = v * Radius;
        }
    }
}

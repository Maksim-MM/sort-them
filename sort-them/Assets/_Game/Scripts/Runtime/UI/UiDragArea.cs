using UnityEngine;
using UnityEngine.EventSystems;

namespace SortThem
{
    public class UiDragArea : MonoBehaviour, IDragHandler, IPointerDownHandler
    {
        public Vector2 Delta { get; private set; }
        public bool Dragging { get; private set; }

        public void OnPointerDown(PointerEventData eventData) => Dragging = true;

        public void OnDrag(PointerEventData eventData)
        {
            Delta += eventData.delta;
        }

        public Vector2 Consume()
        {
            var d = Delta;
            Delta = Vector2.zero;
            return d;
        }
    }
}

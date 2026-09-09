using UnityEngine;
using UnityEngine.EventSystems;

namespace SortThem
{
    public class TouchLookArea : MonoBehaviour, IDragHandler, IPointerDownHandler
    {
        public void OnPointerDown(PointerEventData e) { }

        public void OnDrag(PointerEventData e)
        {
            var gm = GameManager.I;
            if (gm == null || gm.UiBlocking) return;
            TouchInput.AddLook(e.delta / Mathf.Max(1f, Screen.height) * gm.Config.TouchLookSpeed);
        }
    }
}

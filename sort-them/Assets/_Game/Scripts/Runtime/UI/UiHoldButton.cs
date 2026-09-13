using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SortThem
{
    public class UiHoldButton : Selectable
    {
        public bool Held { get; private set; }
        public bool PressedThisFrame { get; private set; }

        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);
            if (!interactable) return;
            Held = true;
            PressedThisFrame = true;
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            base.OnPointerUp(eventData);
            Held = false;
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            base.OnPointerExit(eventData);
            Held = false;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Held = false;
        }

        public void ClearPressed() => PressedThisFrame = false;
    }
}

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SortThem
{
    public class TouchPressButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public TouchButton Button;
        public bool SprintToggle;
        public Image Background;

        static readonly Color Normal = new Color(0.13f, 0.12f, 0.14f, 0.9f);
        static readonly Color Pressed = new Color(0.4f, 0.38f, 0.45f, 0.95f);
        static readonly Color Toggled = new Color(0.35f, 0.55f, 0.95f, 0.95f);

        void OnEnable() => Refresh(false);

        public void OnPointerDown(PointerEventData e)
        {
            if (SprintToggle) TouchInput.SprintToggled = !TouchInput.SprintToggled;
            else TouchInput.Press(Button);
            Refresh(true);
        }

        public void OnPointerUp(PointerEventData e) => Refresh(false);

        void Update()
        {
            if (SprintToggle && Background != null)
            {
                var target = TouchInput.SprintToggled ? Toggled : Normal;
                if (Background.color != target) Background.color = target;
            }
        }

        void Refresh(bool pressed)
        {
            if (Background == null) return;
            Background.color = SprintToggle ? (TouchInput.SprintToggled ? Toggled : Normal) : pressed ? Pressed : Normal;
        }
    }
}

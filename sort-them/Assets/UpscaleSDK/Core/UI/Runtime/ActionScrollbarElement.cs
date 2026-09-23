using UnityEngine;
using UnityEngine.UI;
using UpscaleSDK.Core.Input.Core;
using UpscaleSDK.Core.Input.Definitions;
using UpscaleSDK.Core.Utils;

namespace UpscaleSDK.Core.Ui.Runtime
{
    /// <summary>
    /// Represents a action scrollbar element class.
    /// </summary>
    [RequireComponent(typeof(Scrollbar))]
    public class ActionScrollbarElement : UIElement
    {
        [SerializeField] private Scrollbar _scrollbar;
        [SerializeField] private Vector2ActionDefinition _definition;
        [SerializeField] private float _stepAmount = 0.1f;
        [SerializeField] private bool _checkIsInteractable = true;
        [SerializeField] private bool _checkIsFocused = true;

        private InputAction<Vector2> _inputAction;

        private void Reset()
        {
            _scrollbar = GetComponent<Scrollbar>();
        }

        private void Awake()
        {
            if (_definition == null)
            {
                UPSLogger.UiErrorLog($"{nameof(ActionScrollbarElement)}: Action Definition is not assigned.");
                enabled = false;
                return;
            }
            if (_scrollbar == null) _scrollbar = GetComponent<Scrollbar>();
            if (_scrollbar == null)
            {
                UPSLogger.UiErrorLog($"{nameof(ActionScrollbarElement)}: Scrollbar component missing.");
                enabled = false;
                return;
            }

            _inputAction = _definition.Build();
            _inputAction.Performed += OnPerformed;
        }

        private void OnDestroy()
        {
            _inputAction.Performed -= OnPerformed;
            UPSInput.Unbind(_inputAction.Name);
        }

        private void OnPerformed(Vector2 value)
        {
            if (Layer.IsEnabled == false) return;
            if (IsEnabled == false) return;
            if (_checkIsFocused && IsInFocus == false) return;
            if (_checkIsInteractable && _scrollbar.interactable == false) return;

            float direction = GetAxisForDirection(value);
            if (Mathf.Approximately(direction, 0f)) return;

            float step = _stepAmount * direction;
            float newValue = _scrollbar.value + step;

            if (_stepAmount > 0f)
            {
                newValue = Mathf.Round(newValue / _stepAmount) * _stepAmount;
            }

            if (_scrollbar.numberOfSteps > 1)
            {
                float snap = 1f / (_scrollbar.numberOfSteps - 1);
                newValue = Mathf.Round(newValue / snap) * snap;
            }

            _scrollbar.value = Mathf.Clamp01(newValue);
            Interact();
        }

        private float GetAxisForDirection(Vector2 value)
        {
            // Pick the input axis that matches the scrollbar's orientation, and flip
            // the sign so positive input always drives value toward 1.
            switch (_scrollbar.direction)
            {
                case Scrollbar.Direction.LeftToRight: return value.x;
                case Scrollbar.Direction.RightToLeft: return -value.x;
                case Scrollbar.Direction.BottomToTop: return value.y;
                case Scrollbar.Direction.TopToBottom: return -value.y;
                default: return value.x;
            }
        }
    }
}

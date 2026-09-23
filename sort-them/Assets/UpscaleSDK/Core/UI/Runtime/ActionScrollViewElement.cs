using UnityEngine;
using UnityEngine.UI;
using UpscaleSDK.Core.Input.Core;
using UpscaleSDK.Core.Input.Definitions;
using UpscaleSDK.Core.Utils;

namespace UpscaleSDK.Core.Ui.Runtime
{
    /// <summary>
    /// Represents a action scroll view element class.
    /// </summary>
    [RequireComponent(typeof(ScrollRect))]
    public class ActionScrollViewElement : UIElement
    {
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private Vector2ActionDefinition _definition;
        [SerializeField] private float _stepAmount = 0.1f;
        [SerializeField] private bool _checkIsFocused = true;

        private InputAction<Vector2> _inputAction;

        private void Reset()
        {
            _scrollRect = GetComponent<ScrollRect>();
        }

        private void Awake()
        {
            if (_definition == null)
            {
                UPSLogger.UiErrorLog($"{nameof(ActionScrollViewElement)}: Action Definition is not assigned.");
                enabled = false;
                return;
            }
            if (_scrollRect == null) _scrollRect = GetComponent<ScrollRect>();
            if (_scrollRect == null)
            {
                UPSLogger.UiErrorLog($"{nameof(ActionScrollViewElement)}: ScrollRect component missing.");
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

            Vector2 pos = _scrollRect.normalizedPosition;

            if (_scrollRect.horizontal && Mathf.Approximately(value.x, 0f) == false)
            {
                pos.x = Mathf.Clamp01(pos.x + value.x * _stepAmount);
            }
            if (_scrollRect.vertical && Mathf.Approximately(value.y, 0f) == false)
            {
                pos.y = Mathf.Clamp01(pos.y + value.y * _stepAmount);
            }

            _scrollRect.normalizedPosition = pos;
            Interact();
        }
    }
}

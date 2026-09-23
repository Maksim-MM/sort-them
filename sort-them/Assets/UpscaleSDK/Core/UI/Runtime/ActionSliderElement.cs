using UnityEngine;
using UnityEngine.UI;
using UpscaleSDK.Core.Input.Core;
using UpscaleSDK.Core.Input.Definitions;
using UpscaleSDK.Core.Utils;

namespace UpscaleSDK.Core.Ui.Runtime
{
    /// <summary>
    /// Represents a action slider element class.
    /// </summary>
    public class ActionSliderElement : UIElement
    {
        [SerializeField] private Slider _slider;
        [SerializeField] private Vector2ActionDefinition _definition;
        [SerializeField] private float _stepAmount = 0.1f;
        [SerializeField] private bool _checkIsInteractable = true;
        [SerializeField] private bool _checkIsFocused = true;

        private InputAction<Vector2> _inputAction;
        
        private void Awake()
        {  
            if (_definition == null)
            {
                UPSLogger.UiErrorLog($"{nameof(ActionSliderElement)}: Action Definition is not assigned.");
                enabled = false;
                return;
            }
            if (_slider == null)
            {
                UPSLogger.UiErrorLog($"{nameof(ActionSliderElement)}: Slider component missing.");
                enabled = false;
                return;
            }
            _inputAction = _definition.Build();
            _inputAction.Performed += OnPerformed;
        }

        private void OnDestroy()
        {
            _inputAction.Performed -= OnPerformed;
        }
        
        private void OnPerformed(Vector2 value)
        {
            float direction = value.x;

            if (Layer.IsEnabled == false) return;
            if (IsEnabled == false) return;
            if (_checkIsFocused && IsInFocus == false) return;
            if (_checkIsInteractable && _slider.interactable == false) return;
            
            if (Mathf.Approximately(direction, 0f))
                return;
            
            float step = _stepAmount * direction; 
    
            float newValue = _slider.value + step;

            if (_stepAmount > 0f)
            {
                newValue = Mathf.Round(newValue / _stepAmount) * _stepAmount;
            }

            if (_slider.wholeNumbers)
            {
                newValue = Mathf.Round(newValue);
            }

            _slider.value = Mathf.Clamp(newValue, _slider.minValue, _slider.maxValue);
            Interact();
        }
    }
}
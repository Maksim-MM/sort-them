using UnityEngine;
using UnityEngine.UI;
using UpscaleSDK.Core.Input.Core;
using UpscaleSDK.Core.Input.Definitions;
using UpscaleSDK.Core.Utils;

namespace UpscaleSDK.Core.Ui.Runtime
{
    /// <summary>
    /// Represents a action toggle element class.
    /// </summary>
    [RequireComponent(typeof(Toggle))]
    public class ActionToggleElement : UIElement
    {
        [SerializeField] private Toggle _toggle;
        [SerializeField] private BoolActionDefinition _definition;
        [SerializeField] private bool _checkIsInteractable = true;
        [SerializeField] private bool _checkIsFocused = true;

        private InputAction<bool> _inputAction;

        private void Reset()
        {
            _toggle = GetComponent<Toggle>();
        }

        private void Awake()
        {
            if (_definition == null)
            {
                UPSLogger.UiErrorLog($"{nameof(ActionToggleElement)}: Action Definition is not assigned.");
                enabled = false;
                return;
            }
            if (_toggle == null) _toggle = GetComponent<Toggle>();
            if (_toggle == null)
            {
                UPSLogger.UiErrorLog($"{nameof(ActionToggleElement)}: Toggle component missing.");
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

        private void OnPerformed(bool value)
        {
            if (Layer.IsEnabled == false) return;
            if (IsEnabled == false) return;
            if (_checkIsFocused && IsInFocus == false) return;
            if (_checkIsInteractable && _toggle.interactable == false) return;

            _toggle.isOn = !_toggle.isOn;
            Interact();
        }
    }
}

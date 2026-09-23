using UnityEngine;
using UnityEngine.UI;
using UpscaleSDK.Core.Input.Core;
using UpscaleSDK.Core.Input.Definitions;
using UpscaleSDK.Core.Input.Glyphs.Abstractions;
using UpscaleSDK.Core.Input.Sources.Abstractions;
using UpscaleSDK.Core.Ui.Runtime.Enums;
using UpscaleSDK.Core.Utils;
using DeviceType = UpscaleSDK.Core.Input.Core.Enums.DeviceType;

namespace UpscaleSDK.Core.Ui.Runtime
{
    /// <summary>
    /// Represents a action button element class.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ActionButtonElement : UIElement
    {
        [SerializeField] private BoolActionDefinition _definition;
        [SerializeField] private Button _button;
        [SerializeField] private bool _checkIsInteractable = true;
        [SerializeField] private bool _checkIsFocused = true;

        [Header("Icon")]
        [SerializeField] private bool _showIcon = false;
        [SerializeField] private Image _iconImage;
        [SerializeField] private Corner _corner = Corner.Free;
        [SerializeField] private Vector2 _offset = Vector2.zero;
        [SerializeField] private Vector2 _scale = new Vector2(50f, 50f);
        
        private InputAction<bool> _inputAction;
        private void Awake()
        {
            if (_definition == null)
            {
                UPSLogger.UiErrorLog($"{nameof(ActionButtonElement)}: Action Definition is not assigned.");
                enabled = false;
                return;
            }
            if (_button == null)
            {
                UPSLogger.UiErrorLog($"{nameof(ActionButtonElement)}: Button component missing.");
                enabled = false;
                return;
            }
            
            _inputAction = _definition.Build();
            _inputAction.Performed += OnPerformed;
            UPSInput.OnDeviceAdded += DeviceChanged;
            UPSInput.OnDeviceRemoved += DeviceChanged;
            UpdateIconSprite(UPSInput.GetConnectedDevices());
        }

        private void OnDestroy()
        {
            _inputAction.Performed -= OnPerformed;
            UPSInput.OnDeviceAdded -= DeviceChanged;
            UPSInput.OnDeviceRemoved -= DeviceChanged;
            UPSInput.Unbind(_inputAction.Name);
        }

        private void DeviceChanged(DeviceType type)
        {
            UpdateIconSprite(UPSInput.GetConnectedDevices());
        }

        private void OnPerformed(bool value)
        {
            if (Layer.IsEnabled == false) return;
            if (IsEnabled == false) return;
            if (_checkIsFocused && IsInFocus == false) return;
            if (_checkIsInteractable && _button.interactable == false) return;

            _button.onClick.Invoke();
            Interact();
        }
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            
            UpdateIconSprite(new [] { DeviceType.Gamepad });
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null)
                {
                    ApplyIconPosition();
                }
            };
        }
#endif
        private void UpdateIconSprite(DeviceType[] types)
        {
            if (_iconImage == null)
                return;

            if (_definition == null)
                return;
            
            bool isActive = IsIconActive(types);
            if (isActive )
            {
                IGlyphProvider glyphProvider = UPSInput.TryGetGlyphProvider();
                _iconImage.enabled = true;
                _iconImage.sprite = glyphProvider.TryProvide(_definition.Sources);
            }
            else
            {
                _iconImage.enabled = false;
            }
        }
        private bool IsIconActive(DeviceType[] types)
        {
            if (_showIcon == false)
                return false;

            if (types == null || types.Length == 0)
                return false;

            if (_definition == null)
                return false;

            // Show the icon only if this binding has a source whose device is currently connected.
            // e.g. a gamepad-only binding hides its icon when no gamepad is present.
            foreach (var source in _definition.Sources)
            {
                if (source is not IGlyphSource glyphSource) continue;
                for (int i = 0; i < types.Length; i++)
                {
                    if (glyphSource.Device == types[i]) return true;
                }
            }
            return false;
        }
        private void ApplyIconPosition()
        {
            if (_iconImage == null || _showIcon == false) 
                return;
    
            Vector2 anchor = GetAnchorForCorner(_corner);
            _iconImage.rectTransform.anchorMin = anchor;
            _iconImage.rectTransform.anchorMax = anchor;
            
            _iconImage.rectTransform.anchoredPosition = new Vector2(0f + _offset.x, 0f + _offset.y);    
            _iconImage.rectTransform.sizeDelta = _scale;
        }

        private Vector2 GetAnchorForCorner(Corner corner)
        {
            return corner switch
            {
                Corner.UpperLeft => new Vector2(0f, 1f),
                Corner.UpperCenter => new Vector2(0.5f, 1f),
                Corner.UpperRight => new Vector2(1f, 1f),
                Corner.MiddleLeft => new Vector2(0f, 0.5f),
                Corner.MiddleCenter => new Vector2(0.5f, 0.5f),
                Corner.MiddleRight => new Vector2(1f, 0.5f),
                Corner.LowerLeft => new Vector2(0f, 0f),
                Corner.LowerCenter => new Vector2(0.5f, 0f),
                Corner.LowerRight => new Vector2(1f, 0f),
                Corner.Free => _iconImage.rectTransform.anchorMin,
                _ => new Vector2(0.5f, 0.5f)
            };
        }
    }
}
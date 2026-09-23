using UnityEngine;
using UnityEngine.UI;
using UpscaleSDK.Core.Input.ActionsSet.Default;

namespace UpscaleSDK.Samples.Input.Gamepad
{
    public class GamepadDemo : MonoBehaviour
    {
        [Header("Stick Settings")]
        [SerializeField] private float _stickMovementRange = 50f;
    
        [Header("Visual Colors")]
        [SerializeField] private Color _defaultColor = Color.white;
        [SerializeField] private Color _pressedColor = new Color(1f, 0.5f, 0.5f, 1f);
        [SerializeField] private Color _stickMovedColor = new Color(0.5f, 1f, 0.5f, 1f);

        [Header("Thumbsticks")]
        [SerializeField] private Image _leftStick;   
        [SerializeField] private Image _leftStickPress;
        [SerializeField] private Image _rightStick;       
        [SerializeField] private Image _rightStickPress;  
    
        [Header("Triggers")]
        [SerializeField] private Image _leftTrigger;
        [SerializeField] private Image _rightTrigger;

        [Header("Face Buttons")]
        [SerializeField] private Image _leftButton;
        [SerializeField] private Image _rightButton;
        [SerializeField] private Image _upButton;
        [SerializeField] private Image _downButton;
    
        [Header("D-Pad")]
        [SerializeField] private Image _dpadLeft;
        [SerializeField] private Image _dpadRight;
        [SerializeField] private Image _dpadUp;
        [SerializeField] private Image _dpadDown;
    
        [Header("Shoulders")]
        [SerializeField] private Image _shoulderLeft;
        [SerializeField] private Image _shoulderRight;
    
        [Header("System Buttons")]
        [SerializeField] private Image _startButton;
        [SerializeField] private Image _selectButton;
        [SerializeField] private Image _systemButton;

        private Vector2 _leftStickStartPos;
        private Vector2 _rightStickStartPos;
        private BaseGamepadActionSet _actions;

        private void Start()
        {
            if (_leftStick) _leftStickStartPos = _leftStick.rectTransform.anchoredPosition;
            if (_rightStick) _rightStickStartPos = _rightStick.rectTransform.anchoredPosition;
            _actions = BaseGamepadActionSet.Instance;
        }

        private void Update()
        {
            UpdateStickState(_leftStick, _actions.LeftStick.Read(), _actions.LeftStickButton.Hold.Read(), _leftStickStartPos);
            UpdateStickState(_rightStick, _actions.RightStick.Read(), _actions.RightStickButton.Hold.Read(), _rightStickStartPos);
        
            UpdateTriggerVisual(_leftTrigger, _actions.LeftTrigger.Read());
            UpdateTriggerVisual(_rightTrigger, _actions.RightTrigger.Read());

            UpdateButtonVisual(_leftButton, _actions.West.Hold.Read());
            UpdateButtonVisual(_rightButton, _actions.East.Hold.Read());
            UpdateButtonVisual(_upButton, _actions.North.Hold.Read());
            UpdateButtonVisual(_downButton, _actions.South.Hold.Read());
        
            Vector2 dpad = _actions.DPad.Read();

            UpdateButtonVisual(_dpadUp,    IsPositive(dpad.y));
            UpdateButtonVisual(_dpadDown,  IsNegative(dpad.y));
            UpdateButtonVisual(_dpadRight, IsPositive(dpad.x));
            UpdateButtonVisual(_dpadLeft,  IsNegative(dpad.x));
        
            UpdateButtonVisual(_shoulderLeft, _actions.LeftShoulder.Hold.Read());
            UpdateButtonVisual(_shoulderRight,_actions.RightShoulder.Hold.Read());
        
            UpdateButtonVisual(_startButton, _actions.Start.Hold.Read());
            UpdateButtonVisual(_selectButton, _actions.Select.Hold.Read());
        }
    
        private bool IsPositive(float v, float threshold = 0.5f) => v > threshold;
        private bool IsNegative(float v, float threshold = 0.5f) => v < -threshold;


        private void UpdateStickState(Image stickImg, Vector2 input, bool stickClicked, Vector2 startPos)
        {
            if (stickImg == null) return;
        
            stickImg.rectTransform.anchoredPosition = startPos + (input * _stickMovementRange);
        
            if (stickClicked)
            {
                stickImg.color = _pressedColor;
            }
            else if (input.magnitude > 0.1f)
            {
                stickImg.color = _stickMovedColor;
            }
            else
            {
                stickImg.color = _defaultColor;
            }
        }

        private void UpdateTriggerVisual(Image targetImage, float triggerValue)
        {
            if (targetImage == null) return;
            targetImage.color = Color.Lerp(_defaultColor, _pressedColor, triggerValue);
        }

        private void UpdateButtonVisual(Image targetImage, bool buttonValue)
        {
            if (targetImage == null) return;
            targetImage.color = buttonValue ? _pressedColor : _defaultColor;
        }
    }
}
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UpscaleSDK.Core.Input.Core.Enums;
using UpscaleSDK.Core.Input.Provider.Abstractions;
using DeviceType = UpscaleSDK.Core.Input.Core.Enums.DeviceType;
using MouseButton = UpscaleSDK.Core.Input.Core.Enums.MouseButton;

#if UNITY_PS4
using PlaystationInput = UnityEngine.PS4.PS4Input;
#elif UNITY_PS5
using PlaystationInput = UnityEngine.PS5.PS5Input;
#endif

namespace UpscaleSDK.Core.Input.Provider.NewInputSystem
{
    /// <summary>
    /// Represents a new unity input provider class.
    /// </summary>
    public sealed class NewUnityInputProvider : IInputProvider
    {
        private Gamepad ActiveGamepad => Gamepad.current;
        private readonly List<DeviceType> _deviceCache = new List<DeviceType>();

        /// <summary>
        /// Occurs when device added.
        /// </summary>
        public event Action<DeviceType> OnDeviceAdded;
        /// <summary>
        /// Occurs when device removed.
        /// </summary>
        public event Action<DeviceType> OnDeviceRemoved;

        /// <summary>
        /// Initialize.
        /// </summary>
        public void Initialize()
        {
            InputSystem.onDeviceChange += OnInputDeviceChange;
            //InputSystem.ResetDevice(Mouse.current, true);
        }
        
        public void Dispose()
        {
            InputSystem.onDeviceChange -= OnInputDeviceChange;
        }

        private void OnInputDeviceChange(InputDevice device, InputDeviceChange change)
        {
            //Debug.Log(change);
            if (device is Gamepad)
            {
                if (change == InputDeviceChange.Added)
                {
                    //Debug.Log($"Gamepad ready: {device.name}");
                    OnDeviceAdded?.Invoke(DeviceType.Gamepad);
                }

                if (change == InputDeviceChange.Removed)
                {
                    OnDeviceRemoved?.Invoke(DeviceType.Gamepad);
                }
            }
            else if (device is Keyboard)
            {
                if (change == InputDeviceChange.Added)
                {
                    //Debug.Log($"Keyboard ready: {device.name}");
                    OnDeviceAdded?.Invoke(DeviceType.Keyboard);
                }

                if (change == InputDeviceChange.Removed)
                {
                    OnDeviceRemoved?.Invoke(DeviceType.Keyboard);
                }
            }
            else if (device is Mouse)
            {
                if (change == InputDeviceChange.Added)
                {
                    OnDeviceAdded?.Invoke(DeviceType.Mouse);
                }

                if (change == InputDeviceChange.Removed)
                {
                    OnDeviceRemoved?.Invoke(DeviceType.Mouse);
                }
            }
        }

        /// <summary>
        /// Read button.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="button">The button.</param>
        public bool ReadGamepadButton(int player, GamepadButton button)
        {
            if (ActiveGamepad == null) return false;

            return GetButton(ActiveGamepad, button).isPressed;
        }

        /// <summary>
        /// Read button down.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="button">The button.</param>
        public bool ReadGamepadButtonDown(int player, GamepadButton button)
        {
            if (ActiveGamepad == null) return false;

            return GetButton(ActiveGamepad, button).wasPressedThisFrame;
        }

        /// <summary>
        /// Read button up.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="button">The button.</param>
        public bool ReadGamepadButtonUp(int player, GamepadButton button)
        {
            if (ActiveGamepad == null) return false;

            return GetButton(ActiveGamepad, button).wasReleasedThisFrame;
        }
    
        private ButtonControl GetButton(Gamepad pad, GamepadButton button)
        {
            return button switch
            {
                GamepadButton.ButtonSouth => pad.buttonSouth,
                GamepadButton.ButtonEast  => pad.buttonEast,
                GamepadButton.ButtonWest  => pad.buttonWest,
                GamepadButton.ButtonNorth => pad.buttonNorth,
                GamepadButton.StickLeft => pad.leftStickButton,
                GamepadButton.StickRight =>  pad.rightStickButton,
                GamepadButton.DPadDown => pad.dpad.down,
                GamepadButton.DPadLeft => pad.dpad.left,
                GamepadButton.DPadRight => pad.dpad.right,
                GamepadButton.DPadUp => pad.dpad.up,
                GamepadButton.ShoulderLeft => pad.leftShoulder,
                GamepadButton.ShoulderRight => pad.rightShoulder,
                GamepadButton.Select => pad.selectButton,
                GamepadButton.Start => pad.startButton,
                _ => null
            };
        }

        /// <summary>
        /// Read2 d axis.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="axis">The axis.</param>
        public Vector2 Read2DGamepadAxis(int player, Gamepad2DAxis axis)
        {
            if (ActiveGamepad == null) return Vector2.zero;

            return axis switch
            {
                Gamepad2DAxis.StickLeft => ActiveGamepad.leftStick.ReadValue(),
                Gamepad2DAxis.StickRight => ActiveGamepad.rightStick.ReadValue(),
                Gamepad2DAxis.DPad => ActiveGamepad.dpad.ReadValue(),
                _ => Vector2.zero
            };
        }

        /// <summary>
        /// Read1 d axis.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="axis">The axis.</param>
        public float Read1DGamepadAxis(int player, Gamepad1DAxis axis)
        {
            if (ActiveGamepad == null) return 0;

            return axis switch
            {
                Gamepad1DAxis.TriggerLeft => ActiveGamepad.leftTrigger.ReadValue(),
                Gamepad1DAxis.TriggerRight => ActiveGamepad.rightTrigger.ReadValue(),
                Gamepad1DAxis.StickLeftX => ActiveGamepad.leftStick.ReadValue().x,
                Gamepad1DAxis.StickLeftY => ActiveGamepad.leftStick.ReadValue().y,
                Gamepad1DAxis.StickRightX => ActiveGamepad.rightStick.ReadValue().x,
                Gamepad1DAxis.StickRightY => ActiveGamepad.rightStick.ReadValue().y,
                _ => 0
            };
        }

        /// <summary>
        /// Gets the gamepad type.
        /// </summary>
        /// <param name="player">The player.</param>
        public GamepadType GetGamepadType(int player = 0)
        {
#if UNITY_SWITCH && !UNITY_EDITOR
            return GamepadType.Nintendo;
#elif UNITY_PS4 && !UNITY_EDITOR
            return GamepadType.PlayStation;
#elif UNITY_PS5 && !UNITY_EDITOR
            return GamepadType.PlayStation;
#elif UNITY_GAMECORE_XBOXONE && !UNITY_EDITOR
            return GamepadType.Xbox;
#elif UNITY_GAMECORE_XBOXSERIES && !UNITY_EDITOR
            return GamepadType.Xbox;
#else
            if (ActiveGamepad == null)
                return GamepadType.None;

            string name = ActiveGamepad.name.ToLower();
            string manufacturer = ActiveGamepad.description.manufacturer.ToLower();
            string product = ActiveGamepad.description.product.ToLower();

            if (name.Contains("nintendo") || name.Contains("switch") || manufacturer.Contains("nintendo"))
                return GamepadType.Nintendo;

            if (name.Contains("dualshock") || name.Contains("dualsense") || name.Contains("playstation") || manufacturer.Contains("sony"))
                return GamepadType.PlayStation;

            return GamepadType.Xbox;
#endif
        }

        /// <summary>
        /// Gets the connected devices.
        /// </summary>
        public DeviceType[] GetConnectedDevices()
        {
#if UNITY_SWITCH && !UNITY_EDITOR
            return new[] { DeviceType.Gamepad };
#elif UNITY_PS4 && !UNITY_EDITOR
           return new[] { DeviceType.Gamepad };
#elif UNITY_PS5 && !UNITY_EDITOR
           return new[] { DeviceType.Gamepad };
#elif UNITY_GAMECORE_XBOXONE && !UNITY_EDITOR
            return new[] { DeviceType.Gamepad };
#elif UNITY_GAMECORE_XBOXSERIES && !UNITY_EDITOR
            return new[] { DeviceType.Gamepad };
#endif
            _deviceCache.Clear();

            if (Keyboard.current != null)
            {
                _deviceCache.Add(DeviceType.Keyboard);
            }

            if (Mouse.current != null)
            {
                _deviceCache.Add(DeviceType.Mouse);
            }

            if (ActiveGamepad != null)
            {
                _deviceCache.Add(DeviceType.Gamepad);
            }

            return _deviceCache.ToArray();
        }

        /// <summary>
        /// Read keyboard key held.
        /// </summary>
        public bool ReadKeyboardKey(KeyboardKey keyboardKey)
        {
            var control = GetKeyboardControl(keyboardKey);
            return control != null && control.isPressed;
        }

        /// <summary>
        /// Read keyboard key pressed this frame.
        /// </summary>
        public bool ReadKeyboardKeyDown(KeyboardKey keyboardKey)
        {
            var control = GetKeyboardControl(keyboardKey);
            return control != null && control.wasPressedThisFrame;
        }

        /// <summary>
        /// Read keyboard key released this frame.
        /// </summary>
        public bool ReadKeyboardKeyUp(KeyboardKey keyboardKey)
        {
            var control = GetKeyboardControl(keyboardKey);
            return control != null && control.wasReleasedThisFrame;
        }

        private KeyControl GetKeyboardControl(KeyboardKey keyboardKey)
        {
            var kb = Keyboard.current;
            if (kb == null) return null;

            return keyboardKey switch
            {
                KeyboardKey.Space => kb.spaceKey,
                KeyboardKey.Enter => kb.enterKey,
                KeyboardKey.Escape => kb.escapeKey,
                KeyboardKey.Tab => kb.tabKey,
                KeyboardKey.Backspace => kb.backspaceKey,
                KeyboardKey.Delete => kb.deleteKey,
                KeyboardKey.Insert => kb.insertKey,
                KeyboardKey.Home => kb.homeKey,
                KeyboardKey.End => kb.endKey,
                KeyboardKey.LeftShift => kb.leftShiftKey,
                KeyboardKey.RightShift => kb.rightShiftKey,
                KeyboardKey.LeftCtrl => kb.leftCtrlKey,
                KeyboardKey.RightCtrl => kb.rightCtrlKey,
                KeyboardKey.LeftAlt => kb.leftAltKey,
                KeyboardKey.RightAlt => kb.rightAltKey,
                KeyboardKey.UpArrow => kb.upArrowKey,
                KeyboardKey.DownArrow => kb.downArrowKey,
                KeyboardKey.LeftArrow => kb.leftArrowKey,
                KeyboardKey.RightArrow => kb.rightArrowKey,
                KeyboardKey.A => kb.aKey,
                KeyboardKey.B => kb.bKey,
                KeyboardKey.C => kb.cKey,
                KeyboardKey.D => kb.dKey,
                KeyboardKey.E => kb.eKey,
                KeyboardKey.F => kb.fKey,
                KeyboardKey.G => kb.gKey,
                KeyboardKey.H => kb.hKey,
                KeyboardKey.I => kb.iKey,
                KeyboardKey.J => kb.jKey,
                KeyboardKey.K => kb.kKey,
                KeyboardKey.L => kb.lKey,
                KeyboardKey.M => kb.mKey,
                KeyboardKey.N => kb.nKey,
                KeyboardKey.O => kb.oKey,
                KeyboardKey.P => kb.pKey,
                KeyboardKey.Q => kb.qKey,
                KeyboardKey.R => kb.rKey,
                KeyboardKey.S => kb.sKey,
                KeyboardKey.T => kb.tKey,
                KeyboardKey.U => kb.uKey,
                KeyboardKey.V => kb.vKey,
                KeyboardKey.W => kb.wKey,
                KeyboardKey.X => kb.xKey,
                KeyboardKey.Y => kb.yKey,
                KeyboardKey.Z => kb.zKey,
                KeyboardKey.Digit0 => kb.digit0Key,
                KeyboardKey.Digit1 => kb.digit1Key,
                KeyboardKey.Digit2 => kb.digit2Key,
                KeyboardKey.Digit3 => kb.digit3Key,
                KeyboardKey.Digit4 => kb.digit4Key,
                KeyboardKey.Digit5 => kb.digit5Key,
                KeyboardKey.Digit6 => kb.digit6Key,
                KeyboardKey.Digit7 => kb.digit7Key,
                KeyboardKey.Digit8 => kb.digit8Key,
                KeyboardKey.Digit9 => kb.digit9Key,
                KeyboardKey.F1 => kb.f1Key,
                KeyboardKey.F2 => kb.f2Key,
                KeyboardKey.F3 => kb.f3Key,
                KeyboardKey.F4 => kb.f4Key,
                KeyboardKey.F5 => kb.f5Key,
                KeyboardKey.F6 => kb.f6Key,
                KeyboardKey.F7 => kb.f7Key,
                KeyboardKey.F8 => kb.f8Key,
                KeyboardKey.F9 => kb.f9Key,
                KeyboardKey.F10 => kb.f10Key,
                KeyboardKey.F11 => kb.f11Key,
                KeyboardKey.F12 => kb.f12Key,
                KeyboardKey.Comma => kb.commaKey,
                KeyboardKey.Slash => kb.slashKey,
                KeyboardKey.Semicolon => kb.semicolonKey,
                KeyboardKey.Quote => kb.quoteKey,
                KeyboardKey.LeftBracket => kb.leftBracketKey,
                KeyboardKey.RightBracket => kb.rightBracketKey,
                KeyboardKey.Backslash => kb.backslashKey,
                KeyboardKey.Equals => kb.equalsKey,
                _ => null,
            };
        }

        /// <summary>
        /// Read mouse button held.
        /// </summary>
        public bool ReadMouseButton(MouseButton button)
        {
            var control = GetMouseButtonControl(button);
            return control != null && control.isPressed;
        }

        /// <summary>
        /// Read mouse button pressed this frame.
        /// </summary>
        public bool ReadMouseButtonDown(MouseButton button)
        {
            var control = GetMouseButtonControl(button);
            return control != null && control.wasPressedThisFrame;
        }

        /// <summary>
        /// Read mouse button released this frame.
        /// </summary>
        public bool ReadMouseButtonUp(MouseButton button)
        {
            var control = GetMouseButtonControl(button);
            return control != null && control.wasReleasedThisFrame;
        }

        private ButtonControl GetMouseButtonControl(MouseButton button)
        {
            var mouse = Mouse.current;
            if (mouse == null) return null;

            return button switch
            {
                MouseButton.Left => mouse.leftButton,
                MouseButton.Right => mouse.rightButton,
                MouseButton.Middle => mouse.middleButton,
                _ => null,
            };
        }

        /// <summary>
        /// Read mouse 1D axis.
        /// </summary>
        public float ReadMouse1DAxis(Mouse1DAxis axis)
        {
            var mouse = Mouse.current;
            if (mouse == null) return 0f;

            return axis switch
            {
                Mouse1DAxis.Scroll => mouse.scroll.ReadValue().y,
                Mouse1DAxis.DeltaX => mouse.delta.ReadValue().x,
                Mouse1DAxis.DeltaY => mouse.delta.ReadValue().y,
                Mouse1DAxis.PositionX => GetNormalizedMousePosition().x,
                Mouse1DAxis.PositionY => GetNormalizedMousePosition().y,
                Mouse1DAxis.ScrollUp => Mathf.Max(mouse.scroll.ReadValue().y, 0f),
                Mouse1DAxis.ScrollDown => Mathf.Max(-mouse.scroll.ReadValue().y, 0f),
                _ => 0f,
            };
        }

        /// <summary>
        /// Read mouse 2D axis.
        /// </summary>
        public Vector2 ReadMouse2DAxis(Mouse2DAxis axis)
        {
            var mouse = Mouse.current;
            if (mouse == null) return Vector2.zero;

            return axis switch
            {
                Mouse2DAxis.Delta => mouse.delta.ReadValue(),
                Mouse2DAxis.Position => GetNormalizedMousePosition(),
                _ => Vector2.zero
            };
        }
        
        private Vector2 GetNormalizedMousePosition()
        {
            if (Mouse.current == null)
                return Vector2.zero;
            
            var position = Mouse.current.position.ReadValue();
#if UNITY_EDITOR
            if (position.x < 0 || position.x > Screen.width ||
                position.y < 0 || position.y > Screen.height)
            {
                return Vector2.zero;
            }
#endif
            return position;
        }
        
        /// <summary>
        /// Sets the vibration.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="lowFreq">The low freq.</param>
        /// <param name="highFreq">The high freq.</param>
        public void SetVibration(int player, float lowFreq, float highFreq)
        {
#if UNITY_PS5
    PlaystationInput.PadSetVibrationMode(player, PlaystationInput.VibrationMode.Compatible2);
    PlaystationInput.PadSetVibration(player, (byte)(lowFreq * 255), (byte)(highFreq * 255));
#else
            if (ActiveGamepad != null)
                ActiveGamepad.SetMotorSpeeds(lowFreq, highFreq);
#endif
        }

        /// <summary>
        /// Stop vibration.
        /// </summary>
        /// <param name="player">The player.</param>
        public void StopVibration(int player = 0)
        {

            if (ActiveGamepad != null)
                ActiveGamepad.SetMotorSpeeds(0, 0);
        }
    }
}
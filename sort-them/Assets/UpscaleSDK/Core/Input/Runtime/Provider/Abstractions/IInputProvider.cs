using System;
using UnityEngine;
using UpscaleSDK.Core.Input.Core.Enums;
using DeviceType = UpscaleSDK.Core.Input.Core.Enums.DeviceType;


namespace UpscaleSDK.Core.Input.Provider.Abstractions
{
    /// <summary>
    /// Defines the contract for the input provider.
    /// </summary>
    public interface IInputProvider : IDisposable
    {
        /// <summary>
        /// Occurs when device added.
        /// </summary>
        public event Action<DeviceType> OnDeviceAdded;
        /// <summary>
        /// Occurs when device removed.
        /// </summary>
        public event Action<DeviceType> OnDeviceRemoved;
        void Initialize();
        bool ReadGamepadButton(int player, GamepadButton button);
        bool ReadGamepadButtonDown(int player, GamepadButton button);
        bool ReadGamepadButtonUp(int player, GamepadButton button);
        Vector2 Read2DGamepadAxis(int player, Gamepad2DAxis axis);
        float Read1DGamepadAxis(int player, Gamepad1DAxis axis);
        GamepadType GetGamepadType(int player = 0);
        DeviceType[] GetConnectedDevices();

        bool ReadKeyboardKey(KeyboardKey keyboardKey);
        bool ReadKeyboardKeyDown(KeyboardKey keyboardKey);
        bool ReadKeyboardKeyUp(KeyboardKey keyboardKey);

        bool ReadMouseButton(MouseButton button);
        bool ReadMouseButtonDown(MouseButton button);
        bool ReadMouseButtonUp(MouseButton button);
        float ReadMouse1DAxis(Mouse1DAxis axis);
        Vector2 ReadMouse2DAxis(Mouse2DAxis axis);

        void SetVibration(int player, float lowFreq, float highFreq);
        void StopVibration(int player);
    }
}

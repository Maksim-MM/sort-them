using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UpscaleSDK.Core.Input.Binding;
using UpscaleSDK.Core.Input.Binding.Builders;
using UpscaleSDK.Core.Input.Core.Enums;
using UpscaleSDK.Core.Input.Glyphs;
using UpscaleSDK.Core.Input.Glyphs.Abstractions;
using UpscaleSDK.Core.Input.Glyphs.Enums;
using UpscaleSDK.Core.Input.Provider.Abstractions;
using UpscaleSDK.Core.Input.Provider.NewInputSystem;
using UpscaleSDK.Core.Utils;
using DeviceType = UpscaleSDK.Core.Input.Core.Enums.DeviceType;

namespace UpscaleSDK.Core.Input.Core
{
    /// <summary>
    /// Represents a ups input class.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class UPSInput : MonoBehaviour
    {
        private BindingsRegistry _registry;
        private IInputProvider _inputProvider;
        private IGlyphProvider _glyphProvider;

        private static UPSInput _instance;
        /// <summary>
        /// Occurs when device added.
        /// </summary>
        public static event Action<DeviceType> OnDeviceAdded;
        /// <summary>
        /// Occurs when device removed.
        /// </summary>
        public static event Action<DeviceType> OnDeviceRemoved;
        
        /// <summary>
        /// Initialize.
        /// </summary>
        public void Initialize()
        {
            UPSLogger.InputInfoLog($"Initialized");
            _instance = this;
            _registry = new BindingsRegistry();
            _inputProvider = GetInputProvider();
            _inputProvider.Initialize();
            _inputProvider.OnDeviceAdded += HandleDeviceAdded;
            _inputProvider.OnDeviceRemoved += HandleDeviceRemoved;
            UPSLogger.InputInfoLog($"Use a {_inputProvider.GetType().Name} as provider");
            _glyphProvider = GetGlyphProvider();
        }
        
        private void OnDestroy()
        {

#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall += Deinitialize;
#else
            Deinitialize();
#endif
        }

        private void Deinitialize()
        {
            UPSLogger.InputInfoLog($"Deinitialize");
            StopGamepadVibration();
            _instance = null;
            _registry = null;
            _inputProvider.OnDeviceAdded -= HandleDeviceAdded;
            _inputProvider.OnDeviceRemoved -= HandleDeviceRemoved;
            _inputProvider.Dispose();
            _inputProvider = null;
            _glyphProvider = null;
            
            OnDeviceAdded = null;
            OnDeviceRemoved = null;
        }

        private IInputProvider GetInputProvider()
        {
#if ENABLE_INPUT_SYSTEM
            return new NewUnityInputProvider();
#else 
        return null;
#endif
        }
        private static IGlyphProvider GetGlyphProvider()
        {
            return new GlyphProvider();
        }

        private void HandleDeviceAdded(DeviceType deviceType) => OnDeviceAdded?.Invoke(deviceType);

        private void HandleDeviceRemoved(DeviceType deviceType) => OnDeviceRemoved?.Invoke(deviceType);

        /// <summary>
        /// Subscribe.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="callback">The callback.</param>
        public static void Subscribe<T>(string action, Action<T> callback)
        {
            if (!_instance._registry.TryGetBinding(action, 0, out var obj))
            {
                throw new KeyNotFoundException($"Action '{action}' not registered");
            }

            if (obj is not ActionBinding<T> binding)
            {
                throw new InvalidOperationException($"Type mismatch for action '{action}'.");
            }

            binding.Performed += callback;
        }
    
        /// <summary>
        /// Unsubscribe.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="callback">The callback.</param>
        public static void Unsubscribe<T>(string action, Action<T> callback)
        {
            if (_instance._registry.TryGetBinding(action, 0, out var obj) && obj is ActionBinding<T> binding)
            {
                binding.Performed -= callback;
            }
        }
        
        private void Update()
        {
            if (_inputProvider != null)
            {
                _registry.UpdateAll(_inputProvider);
            }
        }
    
        /// <summary>
        /// Bind as bool.
        /// </summary>
        /// <param name="action">The action.</param>
        public static BindingBuilder<bool> BindAsBool(string action)
        {
            var binding = new BoolActionBinding { Name = action };
            return new BindingBuilder<bool>(binding, _instance._inputProvider, _instance._glyphProvider, _instance._registry);
        }

        /// <summary>
        /// Bind as vector2.
        /// </summary>
        /// <param name="action">The action.</param>
        public static BindingBuilder<Vector2> BindAsVector2(string action)
        {
            var binding = new Vector2ActionBinding { Name = action };
            return new BindingBuilder<Vector2>(binding, _instance._inputProvider, _instance._glyphProvider, _instance._registry);
        }

        /// <summary>
        /// Bind as float.
        /// </summary>
        /// <param name="action">The action.</param>
        public static BindingBuilder<float> BindAsFloat(string action)
        {
            var binding = new FloatActionBinding { Name = action };
            return new BindingBuilder<float>(binding, _instance._inputProvider, _instance._glyphProvider, _instance._registry);
        }
    
        /// <summary>
        /// Sets the action state.
        /// </summary>
        /// <param name="actionName">The action name.</param>
        /// <param name="active">The active.</param>
        /// <param name="player">The player.</param>
        public static void SetActionState(string actionName, bool active, int player = 0)
        {
            if (_instance._registry.TryGetBinding(actionName, player, out var binding))
            {
                binding.SetActive(active);
            }
            else
            {
                throw new KeyNotFoundException($"Can't find action '{actionName}'");
            }
        }
        
        /// <summary>
        /// Unbind.
        /// </summary>
        /// <param name="actionName">The action name.</param>
        public static void Unbind(string actionName)
        {
            _instance._registry.Remove(actionName, 0);
        }

        internal static IReadOnlyList<ActionBinding> GetAllBindings()
        {
            return _instance._registry.GetAll().ToList();
        }

        /// <summary>
        /// Attempts to get glyph provider.
        /// </summary>
        public static IGlyphProvider TryGetGlyphProvider()
        {
            if (_instance != null)
            {
                return _instance._glyphProvider;
            }
            var provider = GetGlyphProvider();
            return provider;
        }
        
        /// <summary>
        /// Get.
        /// </summary>
        /// <param name="action">The action.</param>
        public static ActionResult<T> Get<T>(string action)
        {
            if (!_instance._registry.TryGetProxy<InputAction<T>>(action, 0, out var proxy))
            {
                throw new KeyNotFoundException($"Action '{action}' not found or type mismatch.");
            }
            return new ActionResult<T>(proxy);
        }

        /// <summary>
        /// Get.
        /// </summary>
        /// <param name="action">The action.</param>
        public static ActionResult<T> Get<T>(InputAction<T> action)
        {
            return new ActionResult<T>(action);
        }
        
        /// <summary>
        /// Gets the gamepad type.
        /// </summary>
        /// <param name="player">The player.</param>
        public static GamepadType GetGamepadType(int player = 0)
        {
#if UNITY_EDITOR
            if (_instance == null)
            {
                return GamepadType.Nintendo;
            }
#endif
            return _instance._inputProvider.GetGamepadType(player);
        }
        
        /// <summary>
        /// Sets the gamepad vibration.
        /// </summary>
        /// <param name="low">The low.</param>
        /// <param name="high">The high.</param>
        /// <param name="player">The player.</param>
        public static void SetGamepadVibration(float low, float high, int player = 0)
        {
            _instance._inputProvider.SetVibration(player, low, high);
        }

        /// <summary>
        /// Stop gamepad vibration.
        /// </summary>
        /// <param name="player">The player.</param>
        public static void StopGamepadVibration(int player = 0)
        {
            _instance._inputProvider.StopVibration(player);
        }

        /// <summary>
        /// Gets the connected devices.
        /// </summary>
        public static DeviceType[] GetConnectedDevices()
        {
            return _instance._inputProvider.GetConnectedDevices();
        }

        /// <summary>
        /// Gets the primary device to render glyphs for.
        /// Gamepad wins whenever connected, and is forced on console platforms.
        /// Falls back to Keyboard, then Mouse.
        /// </summary>
        public static DeviceType GetPrimaryDevice()
        {
#if (UNITY_SWITCH || UNITY_PS4 || UNITY_PS5 || UNITY_GAMECORE_XBOXONE || UNITY_GAMECORE_XBOXSERIES) && !UNITY_EDITOR
            return DeviceType.Gamepad;
#else
            if (_instance == null)
            {
                return DeviceType.Gamepad;
            }

            var devices = _instance._inputProvider.GetConnectedDevices();
            bool hasKeyboard = false;
            bool hasMouse = false;

            for (int i = 0; i < devices.Length; i++)
            {
                switch (devices[i])
                {
                    case DeviceType.Gamepad:
                        return DeviceType.Gamepad;
                    case DeviceType.Keyboard:
                        hasKeyboard = true;
                        break;
                    case DeviceType.Mouse:
                        hasMouse = true;
                        break;
                }
            }

            if (hasKeyboard) return DeviceType.Keyboard;
            if (hasMouse) return DeviceType.Mouse;
            return DeviceType.Gamepad;
#endif
        }

        /// <summary>
        /// Attempts to get glyph.
        /// </summary>
        /// <param name="glyph">The glyph.</param>
        public static Sprite TryGetGlyph(GamepadGlyph glyph)
        {
            return _instance._glyphProvider.Provide(glyph);
        }
    }
}
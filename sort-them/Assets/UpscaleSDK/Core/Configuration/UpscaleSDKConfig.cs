using UnityEngine;
using UpscaleSDK.Core.Input.Core.Enums;
using UpscaleSDK.Core.Input.Glyphs;
using UpscaleSDK.Core.Saves.Settings;

namespace UpscaleSDK.Core.Configuration
{
    /// <summary>
    /// Represents a upscale sdk config class.
    /// </summary>
    [CreateAssetMenu(fileName = "Config", menuName = "Upscale SDK/Config", order = 1)]
    public class UpscaleSDKConfig : ScriptableObject
    {
        [SerializeField] private SavesSettings _savesSettings;
       
        [Header("Gamepad icons")]
        [SerializeField] private GamepadIconsSet _ps5Set;
        [SerializeField] private GamepadIconsSet _ps4Set;
        [SerializeField] private GamepadIconsSet _xboxSet;
        [SerializeField] private GamepadIconsSet _switchSet;

        [Header("Keyboard & Mouse icons")]
        [SerializeField] private KeyboardIconsSet _keyboardSet;
        [SerializeField] private MouseIconsSet _mouseSet;

        /// <summary>
        /// Gets the saves settings.
        /// </summary>
        public SavesSettings GetSavesSettings()
        {
            return _savesSettings;
        }

        //[SerializeField] private GamepadType _simulateGamepadType;
        
        // public GamepadType SimaulateGampeadType => _simulateGamepadType;
        /// <summary>
        /// Provide gamepad set.
        /// </summary>
        /// <param name="gamepadType">The gamepad type.</param>
        public GamepadIconsSet ProvideGamepadSet(GamepadType gamepadType)
        {
/*#if UNITY_EDITOR
            if (_simulateGamepadType != GamepadType.None)
            {
                gamepadType = _simulateGamepadType;
            }
#endif*/
#if UNITY_PS5 && !UNITY_EDITOR
                return _ps5Set;
#endif
#if UNITY_PS4 && !UNITY_EDITOR
                return _ps4Set;
#endif
#if UNITY_SWITCH && !UNITY_EDITOR
                return _switchSet;
#endif
#if UNITY_GAMECORE_XBOXSERIES && !UNITY_EDITOR
                return _xboxSet;
#endif
#if UNITY_GAMECORE_XBOXONE && !UNITY_EDITOR
                return _xboxSet;
#endif
            return gamepadType switch
            {
                GamepadType.PlayStation => _ps5Set,
                GamepadType.Nintendo => _switchSet,
                GamepadType.Xbox => _xboxSet,
                _ => _ps5Set
            };
        }

        /// <summary>
        /// Provide keyboard icons set.
        /// </summary>
        public KeyboardIconsSet ProvideKeyboardSet()
        {
            return _keyboardSet;
        }

        /// <summary>
        /// Provide mouse icons set.
        /// </summary>
        public MouseIconsSet ProvideMouseSet()
        {
            return _mouseSet;
        }
    }
}
using System;
using UnityEngine;
using UpscaleSDK.Core.Input.Core;
using UpscaleSDK.Core.Input.Core.Enums;
using UpscaleSDK.Core.Input.Glyphs.Abstractions;
using UpscaleSDK.Core.Input.Glyphs.Enums;
using UpscaleSDK.Core.Input.Provider.Abstractions;
using UpscaleSDK.Core.Input.Sources.Abstractions;
using DeviceType = UpscaleSDK.Core.Input.Core.Enums.DeviceType;

namespace UpscaleSDK.Core.Input.Sources.Gamepad
{
    /// <summary>
    /// Represents a gamepad action source class.
    /// </summary>
    [Serializable]
    public sealed class GamepadActionSource : IInputSource<bool>, IGlyphSource
    {
        [SerializeField] private GamepadAction _action;
        [SerializeField] private ButtonTrigger _trigger;

        public GamepadActionSource(GamepadAction type, ButtonTrigger trigger)
        {
            _action = type;
            _trigger = trigger;
        }

        /// <inheritdoc />
        public DeviceType Device => DeviceType.Gamepad;
    
        /// <summary>
        /// Accumulate.
        /// </summary>
        /// <param name="current">The current.</param>
        /// <param name="provider">The provider.</param>
        /// <param name="player">The player.</param>
        public bool Accumulate(bool current, IInputProvider provider, int player)
        {
            var button = GetButtonForAction(_action);
            
            bool value = _trigger switch
            {
                ButtonTrigger.Hold    => provider.ReadGamepadButton(player, button),
                ButtonTrigger.Press   => provider.ReadGamepadButtonDown(player, button),
                ButtonTrigger.Release => provider.ReadGamepadButtonUp(player, button),
                _ => false
            };

            return current || value;
        }
    
        private GamepadButton GetButtonForAction(GamepadAction type)
        {
            return type switch
            {
                GamepadAction.Apply => GetButtonForApply(0),
                GamepadAction.Start => GamepadButton.Start,
                GamepadAction.Select => GamepadButton.Select,
                GamepadAction.Cancel => GetButtonForCancel(0),
                GamepadAction.BaseAction => GetButtonForBaseAction(0),
                GamepadAction.ExtraAction => GetButtonForExtraAction(0),
                _ => GamepadButton.ButtonSouth
            };
        }
        
        private GamepadButton GetButtonForExtraAction(int player)
        {
            return UPSInput.GetGamepadType(player) switch
            {
                GamepadType.Nintendo    => GamepadButton.ButtonWest, // Y
                GamepadType.PlayStation => GamepadButton.ButtonNorth, // Triangle
                GamepadType.Xbox => GamepadButton.ButtonWest, // X
                _ => GamepadButton.ButtonWest
            };
        }
        
        private GamepadButton GetButtonForBaseAction(int player)
        {
            return UPSInput.GetGamepadType(player) switch
            {
                GamepadType.Nintendo    => GamepadButton.ButtonNorth, // X
                GamepadType.PlayStation => GamepadButton.ButtonWest, // Square
                GamepadType.Xbox => GamepadButton.ButtonNorth, // Y
                _ => GamepadButton.ButtonNorth
            };
        }
        
        private GamepadButton GetButtonForApply(int player)
        {
#if UNITY_PS4 && !UNITY_EDITOR
        if (IsPs4CrossButtonAsEnter())
        {
            return GamepadButton.ButtonSouth;
        }
        else
        {
            return GamepadButton.ButtonEast;
        }
#endif
            return UPSInput.GetGamepadType(player) switch
            {
                GamepadType.Nintendo    => GamepadButton.ButtonEast, // A
                GamepadType.PlayStation => GamepadButton.ButtonSouth, // Cross
                GamepadType.Xbox => GamepadButton.ButtonSouth, // A
                _ => GamepadButton.ButtonSouth
            };
        }
    
        private GamepadButton GetButtonForCancel(int player)
        {
#if UNITY_PS4 && !UNITY_EDITOR
        if (IsPs4CrossButtonAsEnter())
        {
            return GamepadButton.ButtonEast;
        }
        else
        {
            return GamepadButton.ButtonSouth;
        }
#endif
            return UPSInput.GetGamepadType(player) switch
            {
                GamepadType.Nintendo    => GamepadButton.ButtonSouth, // B
                GamepadType.PlayStation => GamepadButton.ButtonEast, // Circle
                GamepadType.Xbox => GamepadButton.ButtonEast, // B
                _ => GamepadButton.ButtonEast
            };
        }
    
#if UNITY_PS4 && !UNITY_EDITOR
        private bool IsPs4CrossButtonAsEnter()
        {
            bool cross = UnityEngine.PS4.Utility.GetSystemServiceParam(UnityEngine.PS4.Utility.SystemServiceParamId.EnterButtonAssign) == 1;
            return cross;
        }
#endif
        /// <summary>
        /// Gets the glyph.
        /// </summary>
        /// <param name="provider">The provider.</param>
        public Sprite GetGlyph(IGlyphProvider provider)
        {
            GamepadButton button = GetButtonForAction(_action);
            var glyph = button switch
            {
                GamepadButton.ButtonEast => GamepadGlyph.East,
                GamepadButton.ButtonSouth => GamepadGlyph.South,
                GamepadButton.ButtonWest => GamepadGlyph.West,
                GamepadButton.ButtonNorth => GamepadGlyph.North,
                GamepadButton.Select => GamepadGlyph.Select,
                GamepadButton.Start => GamepadGlyph.Start,
                _ => GamepadGlyph.South
            };
            return provider.Provide(glyph);
        }
    }
}
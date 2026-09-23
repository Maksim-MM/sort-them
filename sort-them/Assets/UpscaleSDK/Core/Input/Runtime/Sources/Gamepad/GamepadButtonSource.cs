using UnityEngine;
using UpscaleSDK.Core.Input.Core.Enums;
using UpscaleSDK.Core.Input.Glyphs.Abstractions;
using UpscaleSDK.Core.Input.Glyphs.Enums;
using UpscaleSDK.Core.Input.Provider.Abstractions;
using UpscaleSDK.Core.Input.Sources.Abstractions;
using DeviceType = UpscaleSDK.Core.Input.Core.Enums.DeviceType;

namespace UpscaleSDK.Core.Input.Sources.Gamepad
{
    /// <summary>
    /// Represents a gamepad button source class.
    /// </summary>
    [System.Serializable]
    public sealed class GamepadButtonSource : IInputSource<bool>, IGlyphSource
    {
        [SerializeField] private GamepadButton _button;
        [SerializeField] private ButtonTrigger _mode;

        public GamepadButtonSource(GamepadButton button, ButtonTrigger mode)
        {
            _button = button;
            _mode = mode;
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
            bool value = _mode switch
            {
                ButtonTrigger.Hold    => provider.ReadGamepadButton(player, _button),
                ButtonTrigger.Press   => provider.ReadGamepadButtonDown(player, _button),
                ButtonTrigger.Release => provider.ReadGamepadButtonUp(player, _button),
                _ => false
            };

            return current || value;
        }

        /// <summary>
        /// Gets the glyph.
        /// </summary>
        /// <param name="provider">The provider.</param>
        public Sprite GetGlyph(IGlyphProvider provider)
        {
            var glyph = _button switch
            {
                GamepadButton.ButtonEast => GamepadGlyph.East,
                GamepadButton.ButtonSouth => GamepadGlyph.South,
                GamepadButton.ButtonWest => GamepadGlyph.West,
                GamepadButton.ButtonNorth => GamepadGlyph.North,
                GamepadButton.Select => GamepadGlyph.Select,
                GamepadButton.Start => GamepadGlyph.Start,
                GamepadButton.StickLeft => GamepadGlyph.LeftStickButton,
                GamepadButton.StickRight => GamepadGlyph.RightStickButton,
                GamepadButton.ShoulderLeft => GamepadGlyph.ShoulderLeft,
                GamepadButton.ShoulderRight => GamepadGlyph.ShoulderRight,
                GamepadButton.DPadUp => GamepadGlyph.DPadUp,
                GamepadButton.DPadDown => GamepadGlyph.DPadDown,
                GamepadButton.DPadLeft => GamepadGlyph.DPadLeft,
                GamepadButton.DPadRight => GamepadGlyph.DPadRight,
                _ => GamepadGlyph.South
            };
            return provider.Provide(glyph);
        }
    }
}
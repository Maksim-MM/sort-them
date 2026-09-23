using System;
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
    /// Represents a gamepad1 d axis source class.
    /// </summary>
    [Serializable]
    public sealed class Gamepad1DAxisSource : IInputSource<float>, IGlyphSource
    {
        [SerializeField] private Gamepad1DAxis _axis;

        public Gamepad1DAxisSource(Gamepad1DAxis axis)
            => _axis = axis;

        /// <inheritdoc />
        public DeviceType Device => DeviceType.Gamepad;

        /// <summary>
        /// Accumulate.
        /// </summary>
        /// <param name="current">The current.</param>
        /// <param name="provider">The provider.</param>
        /// <param name="player">The player.</param>
        public float Accumulate(float current, IInputProvider provider, int player)
        {
            return current + provider.Read1DGamepadAxis(player, _axis);
        }

        /// <summary>
        /// Gets the glyph.
        /// </summary>
        /// <param name="provider">The provider.</param>
        public Sprite GetGlyph(IGlyphProvider provider)
        {
            var glyph = _axis switch
            {
               Gamepad1DAxis.TriggerLeft => GamepadGlyph.TriggerLeft,
               Gamepad1DAxis.TriggerRight => GamepadGlyph.TriggerRight,
               Gamepad1DAxis.StickLeftX => GamepadGlyph.StickLeftX,
               Gamepad1DAxis.StickRightX => GamepadGlyph.StickRightX,
               Gamepad1DAxis.StickLeftY => GamepadGlyph.StickLeftY,
               Gamepad1DAxis.StickRightY => GamepadGlyph.StickRightY,
                _ => GamepadGlyph.TriggerLeft
            };
            return provider.Provide(glyph);
        }
    }
}
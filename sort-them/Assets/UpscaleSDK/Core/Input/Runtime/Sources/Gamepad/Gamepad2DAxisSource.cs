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
    /// Represents a gamepad2 d axis source class.
    /// </summary>
    [Serializable]
    public sealed class Gamepad2DAxisSource : IInputSource<Vector2>, IGlyphSource
    {
        [SerializeField] private Gamepad2DAxis _axis;

        public Gamepad2DAxisSource(Gamepad2DAxis axis)
            => _axis = axis;

        /// <inheritdoc />
        public DeviceType Device => DeviceType.Gamepad;

        /// <summary>
        /// Accumulate.
        /// </summary>
        /// <param name="current">The current.</param>
        /// <param name="provider">The provider.</param>
        /// <param name="player">The player.</param>
        public Vector2 Accumulate(Vector2 current, IInputProvider provider, int player)
            => current + provider.Read2DGamepadAxis(player, _axis);

        /// <summary>
        /// Gets the glyph.
        /// </summary>
        /// <param name="provider">The provider.</param>
        public Sprite GetGlyph(IGlyphProvider provider)
        {
            var glyph = _axis switch
            {
                Gamepad2DAxis.DPad => GamepadGlyph.Dpad,
                Gamepad2DAxis.StickLeft => GamepadGlyph.StickLeft,
                Gamepad2DAxis.StickRight => GamepadGlyph.StickRight,
                _ => GamepadGlyph.Dpad
            };
            return provider.Provide(glyph);
        }
    }
}
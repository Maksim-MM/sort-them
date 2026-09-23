using System;
using UnityEngine;
using UpscaleSDK.Core.Input.Core.Enums;
using UpscaleSDK.Core.Input.Glyphs.Abstractions;
using UpscaleSDK.Core.Input.Glyphs.Enums;
using UpscaleSDK.Core.Input.Provider.Abstractions;
using UpscaleSDK.Core.Input.Sources.Abstractions;
using DeviceType = UpscaleSDK.Core.Input.Core.Enums.DeviceType;

namespace UpscaleSDK.Core.Input.Sources.Mouse
{
    /// <summary>
    /// Represents a mouse 1D axis source class.
    /// </summary>
    [Serializable]
    public sealed class Mouse1DAxisSource : IInputSource<float>, IGlyphSource
    {
        [SerializeField] private Mouse1DAxis _axis;

        public Mouse1DAxisSource(Mouse1DAxis axis) => _axis = axis;

        /// <inheritdoc />
        public DeviceType Device => DeviceType.Mouse;

        /// <summary>
        /// Accumulate.
        /// </summary>
        public float Accumulate(float current, IInputProvider provider, int player)
        {
            return current + provider.ReadMouse1DAxis(_axis);
        }

        /// <summary>
        /// Gets the glyph.
        /// </summary>
        public Sprite GetGlyph(IGlyphProvider provider)
        {
            var glyph = _axis switch
            {
                Mouse1DAxis.Scroll => MouseGlyph.Scroll,
                Mouse1DAxis.DeltaX => MouseGlyph.MoveX,
                Mouse1DAxis.DeltaY => MouseGlyph.MoveY,
                Mouse1DAxis.PositionX => MouseGlyph.MoveX,
                Mouse1DAxis.PositionY => MouseGlyph.MoveY,
                Mouse1DAxis.ScrollUp => MouseGlyph.ScrollUp,
                Mouse1DAxis.ScrollDown => MouseGlyph.ScrollDown,
                _ => MouseGlyph.Move,
            };
            return provider.Provide(glyph);
        }
    }
}

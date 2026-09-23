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
    /// Represents a mouse 2D axis source class.
    /// </summary>
    [Serializable]
    public sealed class Mouse2DAxisSource : IInputSource<Vector2>, IGlyphSource
    {
        [SerializeField] private Mouse2DAxis _axis;

        public Mouse2DAxisSource(Mouse2DAxis axis) => _axis = axis;

        /// <inheritdoc />
        public DeviceType Device => DeviceType.Mouse;

        /// <summary>
        /// Accumulate.
        /// </summary>
        public Vector2 Accumulate(Vector2 current, IInputProvider provider, int player)
            => current + provider.ReadMouse2DAxis(_axis);

        /// <summary>
        /// Gets the glyph.
        /// </summary>
        public Sprite GetGlyph(IGlyphProvider provider)
        {
            var glyph = _axis switch
            {
                Mouse2DAxis.Delta => MouseGlyph.Move,
                Mouse2DAxis.Position => MouseGlyph.Move,
                _ => MouseGlyph.Move,
            };
            return provider.Provide(glyph);
        }
    }
}

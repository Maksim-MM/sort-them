using UnityEngine;
using DeviceType = UpscaleSDK.Core.Input.Core.Enums.DeviceType;

namespace UpscaleSDK.Core.Input.Glyphs.Abstractions
{
    /// <summary>
    /// Defines the contract for the glyph source.
    /// </summary>
    public interface IGlyphSource
    {
        /// <summary>
        /// The device that owns this glyph.
        /// </summary>
        DeviceType Device { get; }

        Sprite GetGlyph(IGlyphProvider provider);
    }
}

using System.Collections.Generic;
using UnityEngine;
using UpscaleSDK.Core.Input.Glyphs.Enums;
using UpscaleSDK.Core.Input.Sources.Abstractions;

namespace UpscaleSDK.Core.Input.Glyphs.Abstractions
{
    /// <summary>
    /// Defines the contract for the glyph provider.
    /// </summary>
    public interface IGlyphProvider
    {
        /// <summary>
        /// Provide.
        /// </summary>
        /// <param name="glyph">The glyph.</param>
        public Sprite Provide(GamepadGlyph glyph);

        /// <summary>
        /// Provide.
        /// </summary>
        /// <param name="glyph">The glyph.</param>
        public Sprite Provide(KeyboardGlyph glyph);

        /// <summary>
        /// Provide.
        /// </summary>
        /// <param name="glyph">The glyph.</param>
        public Sprite Provide(MouseGlyph glyph);

        /// <summary>
        /// Attempts to provide.
        /// </summary>
        /// <param name="sources">The sources.</param>
        public Sprite TryProvide<T>(List<IInputSource<T>> sources);
    }
}

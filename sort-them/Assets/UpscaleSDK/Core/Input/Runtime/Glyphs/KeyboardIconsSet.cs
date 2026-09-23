using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UpscaleSDK.Core.Input.Glyphs.Enums;

namespace UpscaleSDK.Core.Input.Glyphs
{
    /// <summary>
    /// Represents a keyboard icons set class.
    /// </summary>
    [CreateAssetMenu(fileName = "KeyboardIconsSet", menuName = "Upscale SDK/Keyboard Icons Set", order = 2)]
    public class KeyboardIconsSet : ScriptableObject
    {
        [SerializeField] private List<KeyboardGlyphItem> icons = new();

        /// <summary>
        /// Gets the glyph.
        /// </summary>
        /// <param name="glyph">The glyph.</param>
        public Sprite GetGlyph(KeyboardGlyph glyph)
        {
            return icons.FirstOrDefault(x => x.Glyph == glyph).Sprite;
        }

        [Serializable]
        private struct KeyboardGlyphItem
        {
            /// <summary>
            /// The glyph field.
            /// </summary>
            public KeyboardGlyph Glyph;
            /// <summary>
            /// The sprite field.
            /// </summary>
            public Sprite Sprite;
        }
    }
}

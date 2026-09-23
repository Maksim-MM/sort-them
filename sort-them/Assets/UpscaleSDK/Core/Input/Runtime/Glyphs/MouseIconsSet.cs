using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UpscaleSDK.Core.Input.Glyphs.Enums;

namespace UpscaleSDK.Core.Input.Glyphs
{
    /// <summary>
    /// Represents a mouse icons set class.
    /// </summary>
    [CreateAssetMenu(fileName = "MouseIconsSet", menuName = "Upscale SDK/Mouse Icons Set", order = 3)]
    public class MouseIconsSet : ScriptableObject
    {
        [SerializeField] private List<MouseGlyphItem> icons = new();

        /// <summary>
        /// Gets the glyph.
        /// </summary>
        /// <param name="glyph">The glyph.</param>
        public Sprite GetGlyph(MouseGlyph glyph)
        {
            return icons.FirstOrDefault(x => x.Glyph == glyph).Sprite;
        }

        [Serializable]
        private struct MouseGlyphItem
        {
            /// <summary>
            /// The glyph field.
            /// </summary>
            public MouseGlyph Glyph;
            /// <summary>
            /// The sprite field.
            /// </summary>
            public Sprite Sprite;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UpscaleSDK.Core.Input.Glyphs.Enums;

namespace UpscaleSDK.Core.Input.Glyphs
{
    /// <summary>
    /// Represents a gamepad icons set class.
    /// </summary>
    [CreateAssetMenu(fileName = "IconsSet", menuName = "Upscale SDK/Gamepad Icons Set", order = 1)]
    public class GamepadIconsSet : ScriptableObject
    {
        [SerializeField] private List<GamepadGlyphItem> icons = new();

        /// <summary>
        /// Gets the glyph.
        /// </summary>
        /// <param name="glyph">The glyph.</param>
        public Sprite GetGlyph(GamepadGlyph glyph)
        {
            return icons.FirstOrDefault(x => x.Glyph == glyph).Sprite;
        }
        
        [Serializable]
        private struct GamepadGlyphItem
        {
            /// <summary>
            /// The glyph field.
            /// </summary>
            public GamepadGlyph Glyph;
            /// <summary>
            /// The sprite field.
            /// </summary>
            public Sprite Sprite;
        }
    }
}
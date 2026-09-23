using System;
using UnityEngine;
using UpscaleSDK.Core.Input.Core.Enums;
using UpscaleSDK.Core.Input.Glyphs.Abstractions;
using UpscaleSDK.Core.Input.Glyphs.Enums;
using UpscaleSDK.Core.Input.Provider.Abstractions;
using UpscaleSDK.Core.Input.Sources.Abstractions;
using DeviceType = UpscaleSDK.Core.Input.Core.Enums.DeviceType;

namespace UpscaleSDK.Core.Input.Sources.Keyboard
{
    /// <summary>
    /// Represents a keyboard 2D axis composite source (WASD / Arrows).
    /// </summary>
    [Serializable]
    public sealed class Keyboard2DAxisSource : IInputSource<Vector2>, IGlyphSource
    {
        [SerializeField] private Keyboard2DAxis _axis;

        public Keyboard2DAxisSource(Keyboard2DAxis axis) => _axis = axis;

        /// <inheritdoc />
        public DeviceType Device => DeviceType.Keyboard;

        /// <summary>
        /// Accumulate.
        /// </summary>
        public Vector2 Accumulate(Vector2 current, IInputProvider provider, int player)
        {
            KeyboardKey up, down, left, right;
            switch (_axis)
            {
                case Keyboard2DAxis.WASD:
                    up = KeyboardKey.W; down = KeyboardKey.S;
                    left = KeyboardKey.A; right = KeyboardKey.D;
                    break;
                case Keyboard2DAxis.Arrows:
                default:
                    up = KeyboardKey.UpArrow; down = KeyboardKey.DownArrow;
                    left = KeyboardKey.LeftArrow; right = KeyboardKey.RightArrow;
                    break;
            }

            float x = (provider.ReadKeyboardKey(right) ? 1f : 0f) - (provider.ReadKeyboardKey(left) ? 1f : 0f);
            float y = (provider.ReadKeyboardKey(up) ? 1f : 0f) - (provider.ReadKeyboardKey(down) ? 1f : 0f);
            return current + new Vector2(x, y);
        }

        /// <summary>
        /// Gets the glyph.
        /// </summary>
        public Sprite GetGlyph(IGlyphProvider provider)
        {
            var glyph = _axis switch
            {
                Keyboard2DAxis.WASD => KeyboardGlyph.WASD,
                Keyboard2DAxis.Arrows => KeyboardGlyph.Arrows,
                _ => KeyboardGlyph.WASD,
            };
            return provider.Provide(glyph);
        }
    }
}

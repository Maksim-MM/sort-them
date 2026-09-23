using UnityEngine;
using UnityEngine.Serialization;
using UpscaleSDK.Core.Input.Core.Enums;
using UpscaleSDK.Core.Input.Glyphs.Abstractions;
using UpscaleSDK.Core.Input.Glyphs.Enums;
using UpscaleSDK.Core.Input.Provider.Abstractions;
using UpscaleSDK.Core.Input.Sources.Abstractions;
using DeviceType = UpscaleSDK.Core.Input.Core.Enums.DeviceType;

namespace UpscaleSDK.Core.Input.Sources.Keyboard
{
    /// <summary>
    /// Represents a keyboard button source class.
    /// </summary>
    [System.Serializable]
    public sealed class KeyboardButtonSource : IInputSource<bool>, IGlyphSource
    {
        [SerializeField] private KeyboardKey keyboardKey;
        [SerializeField] private ButtonTrigger _mode;

        public KeyboardButtonSource(KeyboardKey keyboardKey, ButtonTrigger mode)
        {
            this.keyboardKey = keyboardKey;
            _mode = mode;
        }

        /// <inheritdoc />
        public DeviceType Device => DeviceType.Keyboard;

        /// <summary>
        /// Accumulate.
        /// </summary>
        public bool Accumulate(bool current, IInputProvider provider, int player)
        {
            bool value = _mode switch
            {
                ButtonTrigger.Hold => provider.ReadKeyboardKey(keyboardKey),
                ButtonTrigger.Press => provider.ReadKeyboardKeyDown(keyboardKey),
                ButtonTrigger.Release => provider.ReadKeyboardKeyUp(keyboardKey),
                _ => false,
            };

            return current || value;
        }

        /// <summary>
        /// Gets the glyph.
        /// </summary>
        public Sprite GetGlyph(IGlyphProvider provider)
        {
            var glyph = MapKeyToGlyph(keyboardKey);
            return provider.Provide(glyph);
        }

        private static KeyboardGlyph MapKeyToGlyph(KeyboardKey keyboardKey)
        {
            return keyboardKey switch
            {
                KeyboardKey.Space => KeyboardGlyph.Space,
                KeyboardKey.Enter => KeyboardGlyph.Enter,
                KeyboardKey.Escape => KeyboardGlyph.Escape,
                KeyboardKey.Tab => KeyboardGlyph.Tab,
                KeyboardKey.Backspace => KeyboardGlyph.Backspace,
                KeyboardKey.Delete => KeyboardGlyph.Delete,
                KeyboardKey.Insert => KeyboardGlyph.Insert,
                KeyboardKey.Home => KeyboardGlyph.Home,
                KeyboardKey.End => KeyboardGlyph.End,
                KeyboardKey.LeftShift => KeyboardGlyph.LeftShift,
                KeyboardKey.RightShift => KeyboardGlyph.RightShift,
                KeyboardKey.LeftCtrl => KeyboardGlyph.LeftCtrl,
                KeyboardKey.RightCtrl => KeyboardGlyph.RightCtrl,
                KeyboardKey.LeftAlt => KeyboardGlyph.LeftAlt,
                KeyboardKey.RightAlt => KeyboardGlyph.RightAlt,
                KeyboardKey.UpArrow => KeyboardGlyph.UpArrow,
                KeyboardKey.DownArrow => KeyboardGlyph.DownArrow,
                KeyboardKey.LeftArrow => KeyboardGlyph.LeftArrow,
                KeyboardKey.RightArrow => KeyboardGlyph.RightArrow,
                KeyboardKey.A => KeyboardGlyph.A,
                KeyboardKey.B => KeyboardGlyph.B,
                KeyboardKey.C => KeyboardGlyph.C,
                KeyboardKey.D => KeyboardGlyph.D,
                KeyboardKey.E => KeyboardGlyph.E,
                KeyboardKey.F => KeyboardGlyph.F,
                KeyboardKey.G => KeyboardGlyph.G,
                KeyboardKey.H => KeyboardGlyph.H,
                KeyboardKey.I => KeyboardGlyph.I,
                KeyboardKey.J => KeyboardGlyph.J,
                KeyboardKey.K => KeyboardGlyph.K,
                KeyboardKey.L => KeyboardGlyph.L,
                KeyboardKey.M => KeyboardGlyph.M,
                KeyboardKey.N => KeyboardGlyph.N,
                KeyboardKey.O => KeyboardGlyph.O,
                KeyboardKey.P => KeyboardGlyph.P,
                KeyboardKey.Q => KeyboardGlyph.Q,
                KeyboardKey.R => KeyboardGlyph.R,
                KeyboardKey.S => KeyboardGlyph.S,
                KeyboardKey.T => KeyboardGlyph.T,
                KeyboardKey.U => KeyboardGlyph.U,
                KeyboardKey.V => KeyboardGlyph.V,
                KeyboardKey.W => KeyboardGlyph.W,
                KeyboardKey.X => KeyboardGlyph.X,
                KeyboardKey.Y => KeyboardGlyph.Y,
                KeyboardKey.Z => KeyboardGlyph.Z,
                KeyboardKey.Digit0 => KeyboardGlyph.Digit0,
                KeyboardKey.Digit1 => KeyboardGlyph.Digit1,
                KeyboardKey.Digit2 => KeyboardGlyph.Digit2,
                KeyboardKey.Digit3 => KeyboardGlyph.Digit3,
                KeyboardKey.Digit4 => KeyboardGlyph.Digit4,
                KeyboardKey.Digit5 => KeyboardGlyph.Digit5,
                KeyboardKey.Digit6 => KeyboardGlyph.Digit6,
                KeyboardKey.Digit7 => KeyboardGlyph.Digit7,
                KeyboardKey.Digit8 => KeyboardGlyph.Digit8,
                KeyboardKey.Digit9 => KeyboardGlyph.Digit9,
                KeyboardKey.F1 => KeyboardGlyph.F1,
                KeyboardKey.F2 => KeyboardGlyph.F2,
                KeyboardKey.F3 => KeyboardGlyph.F3,
                KeyboardKey.F4 => KeyboardGlyph.F4,
                KeyboardKey.F5 => KeyboardGlyph.F5,
                KeyboardKey.F6 => KeyboardGlyph.F6,
                KeyboardKey.F7 => KeyboardGlyph.F7,
                KeyboardKey.F8 => KeyboardGlyph.F8,
                KeyboardKey.F9 => KeyboardGlyph.F9,
                KeyboardKey.F10 => KeyboardGlyph.F10,
                KeyboardKey.F11 => KeyboardGlyph.F11,
                KeyboardKey.F12 => KeyboardGlyph.F12,
                KeyboardKey.Comma => KeyboardGlyph.Comma,
                KeyboardKey.Slash => KeyboardGlyph.Slash,
                KeyboardKey.Semicolon => KeyboardGlyph.Semicolon,
                KeyboardKey.Quote => KeyboardGlyph.Quote,
                KeyboardKey.LeftBracket => KeyboardGlyph.LeftBracket,
                KeyboardKey.RightBracket => KeyboardGlyph.RightBracket,
                KeyboardKey.Backslash => KeyboardGlyph.Backslash,
                KeyboardKey.Equals => KeyboardGlyph.Equals,
                _ => KeyboardGlyph.E,
            };
        }
    }
}

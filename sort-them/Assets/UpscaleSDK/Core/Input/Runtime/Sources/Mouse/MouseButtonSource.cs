using UnityEngine;
using UpscaleSDK.Core.Input.Core.Enums;
using UpscaleSDK.Core.Input.Glyphs.Abstractions;
using UpscaleSDK.Core.Input.Glyphs.Enums;
using UpscaleSDK.Core.Input.Provider.Abstractions;
using UpscaleSDK.Core.Input.Sources.Abstractions;
using DeviceType = UpscaleSDK.Core.Input.Core.Enums.DeviceType;
using MouseButton = UpscaleSDK.Core.Input.Core.Enums.MouseButton;

namespace UpscaleSDK.Core.Input.Sources.Mouse
{
    /// <summary>
    /// Represents a mouse button source class.
    /// </summary>
    [System.Serializable]
    public sealed class MouseButtonSource : IInputSource<bool>, IGlyphSource
    {
        [SerializeField] private MouseButton _button;
        [SerializeField] private ButtonTrigger _mode;

        public MouseButtonSource(MouseButton button, ButtonTrigger mode)
        {
            _button = button;
            _mode = mode;
        }

        /// <inheritdoc />
        public DeviceType Device => DeviceType.Mouse;

        /// <summary>
        /// Accumulate.
        /// </summary>
        public bool Accumulate(bool current, IInputProvider provider, int player)
        {
            bool value = _mode switch
            {
                ButtonTrigger.Hold => provider.ReadMouseButton(_button),
                ButtonTrigger.Press => provider.ReadMouseButtonDown(_button),
                ButtonTrigger.Release => provider.ReadMouseButtonUp(_button),
                _ => false,
            };

            return current || value;
        }

        /// <summary>
        /// Gets the glyph.
        /// </summary>
        public Sprite GetGlyph(IGlyphProvider provider)
        {
            var glyph = _button switch
            {
                MouseButton.Left => MouseGlyph.Left,
                MouseButton.Right => MouseGlyph.Right,
                MouseButton.Middle => MouseGlyph.Middle,
                _ => MouseGlyph.Left,
            };
            return provider.Provide(glyph);
        }
    }
}

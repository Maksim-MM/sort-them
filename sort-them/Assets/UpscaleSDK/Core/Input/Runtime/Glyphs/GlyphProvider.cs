using System.Collections.Generic;
using UnityEngine;
using UpscaleSDK.Core.Configuration;
using UpscaleSDK.Core.Input.Core;
using UpscaleSDK.Core.Input.Core.Enums;
using UpscaleSDK.Core.Input.Glyphs.Abstractions;
using UpscaleSDK.Core.Input.Glyphs.Enums;
using UpscaleSDK.Core.Input.Sources.Abstractions;
using DeviceType = UpscaleSDK.Core.Input.Core.Enums.DeviceType;

namespace UpscaleSDK.Core.Input.Glyphs
{
    /// <summary>
    /// Represents a glyph provider class.
    /// </summary>
    public class GlyphProvider : IGlyphProvider
    {
        /// <summary>
        /// Provide gamepad glyph.
        /// </summary>
        public Sprite Provide(GamepadGlyph glyph)
        {
            return ConfigurationProvider.GetConfiguration()
                .ProvideGamepadSet(UPSInput.GetGamepadType())
                .GetGlyph(glyph);
        }

        /// <summary>
        /// Provide keyboard glyph.
        /// </summary>
        public Sprite Provide(KeyboardGlyph glyph)
        {
            var set = ConfigurationProvider.GetConfiguration().ProvideKeyboardSet();
            return set == null ? null : set.GetGlyph(glyph);
        }

        /// <summary>
        /// Provide mouse glyph.
        /// </summary>
        public Sprite Provide(MouseGlyph glyph)
        {
            var set = ConfigurationProvider.GetConfiguration().ProvideMouseSet();
            return set == null ? null : set.GetGlyph(glyph);
        }

        /// <summary>
        /// Attempts to provide a glyph for the given sources, preferring the
        /// currently primary device (gamepad has priority when connected or on console).
        /// </summary>
        public Sprite TryProvide<T>(List<IInputSource<T>> sources)
        {
            if (sources == null || sources.Count == 0)
                return null;

            DeviceType primary = UPSInput.GetPrimaryDevice();

            for (int i = sources.Count - 1; i >= 0; i--)
            {
                if (sources[i] is IGlyphSource glyphSource && glyphSource.Device == primary)
                {
                    return glyphSource.GetGlyph(this);
                }
            }

            for (int i = sources.Count - 1; i >= 0; i--)
            {
                if (sources[i] is IGlyphSource glyphSource)
                {
                    return glyphSource.GetGlyph(this);
                }
            }

            return null;
        }
    }
}

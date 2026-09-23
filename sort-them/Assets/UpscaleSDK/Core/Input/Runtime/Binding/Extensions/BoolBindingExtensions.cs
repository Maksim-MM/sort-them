using UpscaleSDK.Core.Input.Binding.Builders;
using UpscaleSDK.Core.Input.Core.Enums;
using UpscaleSDK.Core.Input.Processors;
using UpscaleSDK.Core.Input.Sources.Gamepad;
using UpscaleSDK.Core.Input.Sources.Keyboard;
using UpscaleSDK.Core.Input.Sources.Mouse;

namespace UpscaleSDK.Core.Input.Binding.Extensions
{
    /// <summary>
    /// Represents a bool binding extensions class.
    /// </summary>
    public static class BoolBindingExtensions
    {
        /// <summary>
        /// Converts to button.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="button">The button.</param>
        /// <param name="mode">The mode.</param>
        public static BindingBuilder<bool> ToButton(this BindingBuilder<bool> builder, GamepadButton button, ButtonTrigger mode = ButtonTrigger.Press)
        {
            builder.WithSource(new GamepadButtonSource(button, mode));
            return builder;
        }
        /// <summary>
        /// Converts to action.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="gamepadAction">The gamepad action.</param>
        /// <param name="mode">The mode.</param>
        public static BindingBuilder<bool> ToAction(this BindingBuilder<bool> builder, GamepadAction gamepadAction, ButtonTrigger mode = ButtonTrigger.Press)
        {
            builder.WithSource(new GamepadActionSource(gamepadAction, mode));
            return builder;
        }
        
        /// <summary>
        /// Converts to keyboard key.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="keyboardKey">The key.</param>
        /// <param name="mode">The mode.</param>
        public static BindingBuilder<bool> ToKey(this BindingBuilder<bool> builder, KeyboardKey keyboardKey, ButtonTrigger mode = ButtonTrigger.Press)
        {
            builder.WithSource(new KeyboardButtonSource(keyboardKey, mode));
            return builder;
        }

        /// <summary>
        /// Converts to mouse button.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="button">The button.</param>
        /// <param name="mode">The mode.</param>
        public static BindingBuilder<bool> ToMouseButton(this BindingBuilder<bool> builder, MouseButton button, ButtonTrigger mode = ButtonTrigger.Press)
        {
            builder.WithSource(new MouseButtonSource(button, mode));
            return builder;
        }

        /// <summary>
        /// With invert.
        /// </summary>
        /// <param name="builder">The builder.</param>
        public static BindingBuilder<bool> WithInvert(this BindingBuilder<bool> builder)
        {
            builder.WithProcessor(new InvertBoolProcessor());
            return builder;
        }
    }
}
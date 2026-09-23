using UpscaleSDK.Core.Input.Binding.Builders;
using UpscaleSDK.Core.Input.Core.Enums;
using UpscaleSDK.Core.Input.Processors;
using UpscaleSDK.Core.Input.Sources.Gamepad;
using UpscaleSDK.Core.Input.Sources.Mouse;

namespace UpscaleSDK.Core.Input.Binding.Extensions
{
    /// <summary>
    /// Represents a float binding extensions class.
    /// </summary>
    public static class FloatBindingExtensions
    {
        /// <summary>
        /// Converts to x left stick.
        /// </summary>
        /// <param name="builder">The builder.</param>
        public static BindingBuilder<float> ToXLeftStick(this BindingBuilder<float> builder)
        {
            builder.WithSource(new Gamepad1DAxisSource(Gamepad1DAxis.StickLeftX));
            return builder;
        }
        /// <summary>
        /// Converts to y left stick.
        /// </summary>
        /// <param name="builder">The builder.</param>
        public static BindingBuilder<float> ToYLeftStick(this BindingBuilder<float> builder)
        {
            builder.WithSource(new Gamepad1DAxisSource(Gamepad1DAxis.StickLeftY));
            return builder;
        }
        /// <summary>
        /// Converts to x right stick.
        /// </summary>
        /// <param name="builder">The builder.</param>
        public static BindingBuilder<float> ToXRightStick(this BindingBuilder<float> builder)
        {
            builder.WithSource(new Gamepad1DAxisSource(Gamepad1DAxis.StickRightX));
            return builder;
        }
        /// <summary>
        /// Converts to y right stick.
        /// </summary>
        /// <param name="builder">The builder.</param>
        public static BindingBuilder<float> ToYRightStick(this BindingBuilder<float> builder)
        {
            builder.WithSource(new Gamepad1DAxisSource(Gamepad1DAxis.StickRightY));
            return builder;
        }
        /// <summary>
        /// Converts to left trigger.
        /// </summary>
        /// <param name="builder">The builder.</param>
        public static BindingBuilder<float> ToLeftTrigger(this BindingBuilder<float> builder)
        {
            builder.WithSource(new Gamepad1DAxisSource(Gamepad1DAxis.TriggerLeft));
            return builder;
        }
    
        /// <summary>
        /// Converts to right trigger.
        /// </summary>
        /// <param name="builder">The builder.</param>
        public static BindingBuilder<float> ToRightTrigger(this BindingBuilder<float> builder)
        {
            builder.WithSource(new Gamepad1DAxisSource(Gamepad1DAxis.TriggerRight));
            return builder;
        }
        
        /// <summary>
        /// Converts to a mouse 1D axis.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="axis">The axis.</param>
        public static BindingBuilder<float> ToMouseAxis(this BindingBuilder<float> builder, Mouse1DAxis axis)
        {
            builder.WithSource(new Mouse1DAxisSource(axis));
            return builder;
        }

        /// <summary>
        /// With invert.
        /// </summary>
        /// <param name="builder">The builder.</param>
        public static BindingBuilder<float> WithInvert(this BindingBuilder<float> builder)
        {
            builder.WithProcessor(new InvertFloatProcessor());
            return builder;
        }
    }
}
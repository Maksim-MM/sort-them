using UnityEngine;
using UpscaleSDK.Core.Input.Binding.Builders;
using UpscaleSDK.Core.Input.Core.Enums;
using UpscaleSDK.Core.Input.Processors;
using UpscaleSDK.Core.Input.Sources.Gamepad;
using UpscaleSDK.Core.Input.Sources.Keyboard;
using UpscaleSDK.Core.Input.Sources.Mouse;

namespace UpscaleSDK.Core.Input.Binding.Extensions
{
    /// <summary>
    /// Represents a vector2 binding extensions class.
    /// </summary>
    public static class Vector2BindingExtensions
    {
        /// <summary>
        /// Converts to left stick.
        /// </summary>
        /// <param name="builder">The builder.</param>
        public static BindingBuilder<Vector2> ToLeftStick(this BindingBuilder<Vector2> builder)
        {
            builder.WithSource(new Gamepad2DAxisSource(Gamepad2DAxis.StickLeft));
            return builder;
        }

        /// <summary>
        /// Converts to right stick.
        /// </summary>
        /// <param name="builder">The builder.</param>
        public static BindingBuilder<Vector2> ToRightStick(this BindingBuilder<Vector2> builder)
        {
            builder.WithSource(new Gamepad2DAxisSource(Gamepad2DAxis.StickRight));
            return builder;
        }

        /// <summary>
        /// Converts to dpad.
        /// </summary>
        /// <param name="builder">The builder.</param>
        public static BindingBuilder<Vector2> ToDpad(this BindingBuilder<Vector2> builder)
        {
            builder.WithSource(new Gamepad2DAxisSource(Gamepad2DAxis.DPad));
            return builder;
        }

        /// <summary>
        /// Converts to a mouse 2D axis.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="axis">The axis.</param>
        public static BindingBuilder<Vector2> ToMouseAxis(this BindingBuilder<Vector2> builder, Mouse2DAxis axis)
        {
            builder.WithSource(new Mouse2DAxisSource(axis));
            return builder;
        }

        /// <summary>
        /// Converts to WASD keys (X = D - A, Y = W - S).
        /// </summary>
        /// <param name="builder">The builder.</param>
        public static BindingBuilder<Vector2> ToWASD(this BindingBuilder<Vector2> builder)
        {
            builder.WithSource(new Keyboard2DAxisSource(Keyboard2DAxis.WASD));
            return builder;
        }

        /// <summary>
        /// Converts to arrow keys (X = Right - Left, Y = Up - Down).
        /// </summary>
        /// <param name="builder">The builder.</param>
        public static BindingBuilder<Vector2> ToArrows(this BindingBuilder<Vector2> builder)
        {
            builder.WithSource(new Keyboard2DAxisSource(Keyboard2DAxis.Arrows));
            return builder;
        }

        /// <summary>
        /// With deadzone.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="value">The value.</param>
        public static BindingBuilder<Vector2> WithDeadzone(this BindingBuilder<Vector2> builder, float value)
        {
            builder.WithProcessor(new DeadzoneVector2Processor(value));
            return builder;
        }

        /// <summary>
        /// With invert y.
        /// </summary>
        /// <param name="builder">The builder.</param>
        public static BindingBuilder<Vector2> WithInvertY(this BindingBuilder<Vector2> builder)
        {
            builder.WithProcessor(new InvertYVector2Processor());
            return builder;
        }
        
        /// <summary>
        /// With invert x.
        /// </summary>
        /// <param name="builder">The builder.</param>
        public static BindingBuilder<Vector2> WithInvertX(this BindingBuilder<Vector2> builder)
        {
            builder.WithProcessor(new InvertXVector2Processor());
            return builder;
        }
    }
}
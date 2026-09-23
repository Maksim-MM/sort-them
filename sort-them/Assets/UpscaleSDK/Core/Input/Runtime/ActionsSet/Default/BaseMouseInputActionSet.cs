using UnityEngine;
using UpscaleSDK.Core.Input.ActionsSet.Builders;
using UpscaleSDK.Core.Input.ActionsSet.Extensions;
using UpscaleSDK.Core.Input.ActionsSet.Groups;
using UpscaleSDK.Core.Input.Binding.Extensions;
using UpscaleSDK.Core.Input.Core;
using UpscaleSDK.Core.Input.Core.Enums;

namespace UpscaleSDK.Core.Input.ActionsSet.Default
{
    /// <summary>
    /// Mouse actions for <see cref="BaseGamepadActionSet"/>.
    /// </summary>
    public class BaseMouseInputActionSet : InputActionsSet<BaseMouseInputActionSet>
    {
        /// <summary>Left mouse button.</summary>
        public ButtonActionGroup LeftClick { get; private set; }
        /// <summary>Right mouse button.</summary>
        public ButtonActionGroup RightClick { get; private set; }
        /// <summary>Middle mouse button.</summary>
        public ButtonActionGroup MiddleClick { get; private set; }

        /// <summary>Signed mouse wheel scroll (up positive, down negative).</summary>
        public InputAction<float> Scroll { get; private set; }
        /// <summary>Unsigned scroll up magnitude.</summary>
        public InputAction<float> ScrollUp { get; private set; }
        /// <summary>Unsigned scroll down magnitude.</summary>
        public InputAction<float> ScrollDown { get; private set; }

        /// <summary>Mouse movement delta this frame.</summary>
        public InputAction<Vector2> MouseDelta { get; private set; }
        /// <summary>Mouse cursor position in screen pixels.</summary>
        public InputAction<Vector2> MousePosition { get; private set; }

        /// <summary>Mouse delta X component.</summary>
        public InputAction<float> MouseDeltaX { get; private set; }
        /// <summary>Mouse delta Y component.</summary>
        public InputAction<float> MouseDeltaY { get; private set; }
        /// <summary>Mouse position X in screen pixels.</summary>
        public InputAction<float> MousePositionX { get; private set; }
        /// <summary>Mouse position Y in screen pixels.</summary>
        public InputAction<float> MousePositionY { get; private set; }

        protected override void Initialize(InputSetBuilder builder)
        {
            LeftClick = builder.BindMouseButtonGroup(MouseButton.Left);
            RightClick = builder.BindMouseButtonGroup(MouseButton.Right);
            MiddleClick = builder.BindMouseButtonGroup(MouseButton.Middle);

            Scroll = builder.BindAsFloat().ToMouseAxis(Mouse1DAxis.Scroll).Complete();
            ScrollUp = builder.BindAsFloat().ToMouseAxis(Mouse1DAxis.ScrollUp).Complete();
            ScrollDown = builder.BindAsFloat().ToMouseAxis(Mouse1DAxis.ScrollDown).Complete();

            MouseDelta = builder.BindAsVector2().ToMouseAxis(Mouse2DAxis.Delta).Complete();
            MousePosition = builder.BindAsVector2().ToMouseAxis(Mouse2DAxis.Position).Complete();

            MouseDeltaX = builder.BindAsFloat().ToMouseAxis(Mouse1DAxis.DeltaX).Complete();
            MouseDeltaY = builder.BindAsFloat().ToMouseAxis(Mouse1DAxis.DeltaY).Complete();
            MousePositionX = builder.BindAsFloat().ToMouseAxis(Mouse1DAxis.PositionX).Complete();
            MousePositionY = builder.BindAsFloat().ToMouseAxis(Mouse1DAxis.PositionY).Complete();
        }
    }
}

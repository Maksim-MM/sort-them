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
    /// Represents a base input action set class.
    /// </summary>
    public class BaseGamepadActionSet : InputActionsSet<BaseGamepadActionSet>
    {
        /// <summary>
        /// Gets or sets the apply.
        /// </summary>
        public ButtonActionGroup Apply { get; private set; }
        /// <summary>
        /// Gets or sets the cancel.
        /// </summary>
        public ButtonActionGroup Cancel { get; private set; }
        /// <summary>
        /// Gets or sets the base.
        /// </summary>
        public ButtonActionGroup Base { get; private set; }
        /// <summary>
        /// Gets or sets the extra.
        /// </summary>
        public ButtonActionGroup Extra { get; private set; }
        /// <summary>
        /// Gets or sets the select.
        /// </summary>
        public ButtonActionGroup Select { get; private set; }
        /// <summary>
        /// Gets or sets the start.
        /// </summary>
        public ButtonActionGroup Start { get; private set; }
        /// <summary>
        /// Gets or sets the south.
        /// </summary>
        public ButtonActionGroup South { get; private set; }
        /// <summary>
        /// Gets or sets the west.
        /// </summary>
        public ButtonActionGroup West { get; private set; }
        /// <summary>
        /// Gets or sets the north.
        /// </summary>
        public ButtonActionGroup North { get; private set; }
        /// <summary>
        /// Gets or sets the east.
        /// </summary>
        public ButtonActionGroup East { get; private set; }
        /// <summary>
        /// Gets or sets the left shoulder.
        /// </summary>
        public ButtonActionGroup LeftShoulder { get; private set; }
        /// <summary>
        /// Gets or sets the right shoulder.
        /// </summary>
        public ButtonActionGroup RightShoulder { get; private set; }
        /// <summary>
        /// Gets or sets the left stick button.
        /// </summary>
        public ButtonActionGroup LeftStickButton { get; private set; } 
        /// <summary>
        /// Gets or sets the right stick button.
        /// </summary>
        public ButtonActionGroup RightStickButton { get; private set; } 
        /// <summary>
        /// Gets or sets the left stick.
        /// </summary>
        public InputAction<Vector2> LeftStick { get; private set; }
        /// <summary>
        /// Gets or sets the right stick.
        /// </summary>
        public InputAction<Vector2> RightStick { get; private set; }
        
        /// <summary>
        /// Gets or sets the left stick x.
        /// </summary>
        public InputAction<float> LeftStickX { get; private set; }
        /// <summary>
        /// Gets or sets the left stick y.
        /// </summary>
        public InputAction<float> LeftStickY { get; private set; }
        /// <summary>
        /// Gets or sets the right stick x.
        /// </summary>
        public InputAction<float> RightStickX { get; private set; }
        /// <summary>
        /// Gets or sets the right stick y.
        /// </summary>
        public InputAction<float> RightStickY { get; private set; }
        /// <summary>
        /// Gets or sets the d pad.
        /// </summary>
        public InputAction<Vector2> DPad { get; private set; }
        /// <summary>
        /// Gets or sets the dpad up.
        /// </summary>
        public ButtonActionGroup DpadUp { get; private set; }
        /// <summary>
        /// Gets or sets the dpad down.
        /// </summary>
        public ButtonActionGroup DpadDown { get; private set; }
        /// <summary>
        /// Gets or sets the dpad left.
        /// </summary>
        public ButtonActionGroup DpadLeft { get; private set; }
        /// <summary>
        /// Gets or sets the dpad right.
        /// </summary>
        public ButtonActionGroup DpadRight { get; private set; }
        /// <summary>
        /// Gets or sets the left trigger.
        /// </summary>
        public InputAction<float> LeftTrigger { get; private set; }
        /// <summary>
        /// Gets or sets the right trigger.
        /// </summary>
        public InputAction<float> RightTrigger { get; private set; }

        /// <summary>
        /// Initialize.
        /// </summary>
        /// <param name="builder">The builder.</param>
        protected override void Initialize(InputSetBuilder builder)
        {

            Apply = builder.BindActionGroup(GamepadAction.Apply);
            Cancel = builder.BindActionGroup(GamepadAction.Cancel);
            Base = builder.BindActionGroup(GamepadAction.BaseAction);
            Extra = builder.BindActionGroup(GamepadAction.ExtraAction);
            Select = builder.BindActionGroup(GamepadAction.Select);
            Start = builder.BindActionGroup(GamepadAction.Start);

            South = builder.BindButtonGroup(GamepadButton.ButtonSouth);
            West = builder.BindButtonGroup(GamepadButton.ButtonWest);
            North = builder.BindButtonGroup(GamepadButton.ButtonNorth);
            East = builder.BindButtonGroup(GamepadButton.ButtonEast);
            
            LeftShoulder = builder.BindButtonGroup(GamepadButton.ShoulderLeft);
            RightShoulder = builder.BindButtonGroup(GamepadButton.ShoulderRight);
            
            LeftStickButton = builder.BindButtonGroup(GamepadButton.StickLeft);
            RightStickButton = builder.BindButtonGroup(GamepadButton.StickRight);

            LeftTrigger = builder.BindAsFloat().ToLeftTrigger().Complete();
            RightTrigger = builder.BindAsFloat().ToRightTrigger().Complete();
            LeftStickX = builder.BindAsFloat().ToXLeftStick().Complete();
            RightStickX = builder.BindAsFloat().ToXRightStick().Complete();
            LeftStickY = builder.BindAsFloat().ToYLeftStick().Complete();
            RightStickY = builder.BindAsFloat().ToYRightStick().Complete();
            
            LeftStick = builder.BindAsVector2().ToLeftStick().Complete();
            RightStick = builder.BindAsVector2().ToRightStick().Complete();
            
            DPad = builder.BindAsVector2().ToDpad().Complete();
            DpadUp = builder.BindButtonGroup(GamepadButton.DPadUp);
            DpadDown = builder.BindButtonGroup(GamepadButton.DPadDown);
            DpadLeft = builder.BindButtonGroup(GamepadButton.DPadLeft);
            DpadRight = builder.BindButtonGroup(GamepadButton.DPadRight);
        }
    }
}
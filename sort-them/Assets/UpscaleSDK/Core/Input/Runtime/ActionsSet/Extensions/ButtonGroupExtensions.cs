using UpscaleSDK.Core.Input.ActionsSet.Builders;
using UpscaleSDK.Core.Input.ActionsSet.Groups;
using UpscaleSDK.Core.Input.Binding.Extensions;
using UpscaleSDK.Core.Input.Core.Enums;

namespace UpscaleSDK.Core.Input.ActionsSet.Extensions
{
    /// <summary>
    /// Represents a button group extensions class.
    /// </summary>
    public static class ButtonGroupExtensions
    {
        /// <summary>
        /// Bind action group.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="action">The action.</param>
        public static ButtonActionGroup BindActionGroup(this InputSetBuilder builder, GamepadAction action)
        {
            return new ButtonActionGroup(
                builder.BindAsBool().ToAction(action).Complete(),
                builder.BindAsBool().ToAction(action, ButtonTrigger.Release).Complete(),
                builder.BindAsBool().ToAction(action, ButtonTrigger.Hold).Complete()
            );
        }
        
        /// <summary>
        /// Bind button group.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="button">The button.</param>
        public static ButtonActionGroup BindButtonGroup(this InputSetBuilder builder, GamepadButton button)
        {
            return new ButtonActionGroup(
                builder.BindAsBool().ToButton(button).Complete(),
                builder.BindAsBool().ToButton(button, ButtonTrigger.Release).Complete(),
                builder.BindAsBool().ToButton(button, ButtonTrigger.Hold).Complete()
            );
        }

        /// <summary>
        /// Bind keyboard key group.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="key">The key.</param>
        public static ButtonActionGroup BindKeyGroup(this InputSetBuilder builder, KeyboardKey key)
        {
            return new ButtonActionGroup(
                builder.BindAsBool().ToKey(key).Complete(),
                builder.BindAsBool().ToKey(key, ButtonTrigger.Release).Complete(),
                builder.BindAsBool().ToKey(key, ButtonTrigger.Hold).Complete()
            );
        }

        /// <summary>
        /// Bind mouse button group.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="button">The button.</param>
        public static ButtonActionGroup BindMouseButtonGroup(this InputSetBuilder builder, MouseButton button)
        {
            return new ButtonActionGroup(
                builder.BindAsBool().ToMouseButton(button).Complete(),
                builder.BindAsBool().ToMouseButton(button, ButtonTrigger.Release).Complete(),
                builder.BindAsBool().ToMouseButton(button, ButtonTrigger.Hold).Complete()
            );
        }
    }
}
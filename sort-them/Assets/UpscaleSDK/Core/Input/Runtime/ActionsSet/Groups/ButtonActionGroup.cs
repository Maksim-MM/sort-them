using UpscaleSDK.Core.Input.Core;

namespace UpscaleSDK.Core.Input.ActionsSet.Groups
{
    /// <summary>
    /// Represents a button action group class.
    /// </summary>
    public sealed class ButtonActionGroup
    {
        /// <summary>
        /// Gets or sets the press.
        /// </summary>
        public InputAction<bool> Press { get; }
        /// <summary>
        /// Gets or sets the release.
        /// </summary>
        public InputAction<bool> Release { get; }
        /// <summary>
        /// Gets or sets the hold.
        /// </summary>
        public InputAction<bool> Hold { get; }

        public ButtonActionGroup(
            InputAction<bool> press,
            InputAction<bool> release,
            InputAction<bool> hold)
        {
            Press = press;
            Release = release;
            Hold = hold;
        }
    }
}
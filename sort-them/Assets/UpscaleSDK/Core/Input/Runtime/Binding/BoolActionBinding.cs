using UpscaleSDK.Core.Input.Provider.Abstractions;

namespace UpscaleSDK.Core.Input.Binding
{
    /// <summary>
    /// Represents a bool action binding class.
    /// </summary>
    public sealed class BoolActionBinding : ActionBinding<bool>
    {
        /// <summary>
        /// Refresh.
        /// </summary>
        /// <param name="provider">The provider.</param>
        public override void Refresh(IInputProvider provider)
        {
            bool value = Read(provider);
        
            if (value)
            {
                InvokePerformed(true);
            }
        }
    }
}
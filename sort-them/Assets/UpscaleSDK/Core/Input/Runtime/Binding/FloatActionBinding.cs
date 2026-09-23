using UnityEngine;
using UpscaleSDK.Core.Input.Provider.Abstractions;

namespace UpscaleSDK.Core.Input.Binding
{
    /// <summary>
    /// Represents a float action binding class.
    /// </summary>
    public sealed class FloatActionBinding : ActionBinding<float>
    {
        private float _previous;
        private const float Threshold = 0.0001f;

        /// <summary>
        /// Refresh.
        /// </summary>
        /// <param name="provider">The provider.</param>
        public override void Refresh(IInputProvider provider)
        {
            float current = Read(provider);
        

            if (Mathf.Abs(current - _previous) > Threshold)
            {
                InvokePerformed(current);
            }

            _previous = current;
        }
    }
}
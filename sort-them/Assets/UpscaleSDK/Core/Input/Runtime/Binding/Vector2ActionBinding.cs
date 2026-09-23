using UnityEngine;
using UpscaleSDK.Core.Input.Provider.Abstractions;

namespace UpscaleSDK.Core.Input.Binding
{
    /// <summary>
    /// Represents a vector2 action binding class.
    /// </summary>
    public sealed class Vector2ActionBinding : ActionBinding<Vector2>
    {
        private Vector2 _previous;

        private const float DeadzoneSqr = 0.0001f;

        /// <summary>
        /// Refresh.
        /// </summary>
        /// <param name="provider">The provider.</param>
        public override void Refresh(IInputProvider provider)
        {
            Vector2 current = Read(provider);
        
            bool wasActive = _previous.sqrMagnitude > DeadzoneSqr;
            bool isActive  = current.sqrMagnitude > DeadzoneSqr;

            if (wasActive || isActive)
            {
                if ((_previous - current).sqrMagnitude > DeadzoneSqr)
                {
                    InvokePerformed(current);
                }
            }

            _previous = current;
        }
    }
}
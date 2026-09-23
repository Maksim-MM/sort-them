using UnityEngine;
using UpscaleSDK.Core.Input.Processors.Abstractions;

namespace UpscaleSDK.Core.Input.Processors
{
    /// <summary>
    /// Represents a deadzone vector2 processor class.
    /// </summary>
    public sealed class DeadzoneVector2Processor : IProcessor<Vector2>
    {
        private readonly float _deadzone;

        public DeadzoneVector2Processor(float deadzone)
            => _deadzone = deadzone;

        /// <summary>
        /// Process.
        /// </summary>
        /// <param name="value">The value.</param>
        public Vector2 Process(Vector2 value)
            => value.magnitude < _deadzone ? Vector2.zero : Vector2.ClampMagnitude(value, 1f);

        /// <summary>
        /// Clone.
        /// </summary>
        public IProcessor<Vector2> Clone()
        {
            //return new DeadzoneVector2Processor(_deadzone);
            return (IProcessor<Vector2>)MemberwiseClone();
        }
    }
}

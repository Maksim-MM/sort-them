using UnityEngine;
using UpscaleSDK.Core.Input.Processors.Abstractions;

namespace UpscaleSDK.Core.Input.Processors
{
    /// <summary>
    /// Represents a invert y vector2 processor class.
    /// </summary>
    public sealed class InvertYVector2Processor : IProcessor<Vector2>
    {
        /// <summary>
        /// Process.
        /// </summary>
        /// <param name="value">The value.</param>
        public Vector2 Process(Vector2 value)
            => new Vector2(value.x, -value.y);

        /// <summary>
        /// Clone.
        /// </summary>
        public IProcessor<Vector2> Clone()
        {
            return (IProcessor<Vector2>)MemberwiseClone();
        }
    }
}

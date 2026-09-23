using UpscaleSDK.Core.Input.Processors.Abstractions;

namespace UpscaleSDK.Core.Input.Processors
{
    /// <summary>
    /// Represents a invert float processor class.
    /// </summary>
    public sealed class InvertFloatProcessor : IProcessor<float>
    {
        /// <summary>
        /// Process.
        /// </summary>
        /// <param name="value">The value.</param>
        public float Process(float value)
            => -value;

        /// <summary>
        /// Clone.
        /// </summary>
        public IProcessor<float> Clone()
        {
            return (IProcessor<float>)MemberwiseClone();
        }
    }
}
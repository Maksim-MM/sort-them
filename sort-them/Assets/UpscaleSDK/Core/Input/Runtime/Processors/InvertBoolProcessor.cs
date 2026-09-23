using UpscaleSDK.Core.Input.Processors.Abstractions;

namespace UpscaleSDK.Core.Input.Processors
{
    /// <summary>
    /// Represents a invert bool processor class.
    /// </summary>
    public sealed class InvertBoolProcessor : IProcessor<bool>
    {
        /// <summary>
        /// Process.
        /// </summary>
        /// <param name="value">The value.</param>
        public bool Process(bool value)
            => !value;

        /// <summary>
        /// Clone.
        /// </summary>
        public IProcessor<bool> Clone()
        {
            return (IProcessor<bool>)MemberwiseClone();
        }
    }
}
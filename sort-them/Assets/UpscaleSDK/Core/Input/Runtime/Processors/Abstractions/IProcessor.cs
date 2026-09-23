namespace UpscaleSDK.Core.Input.Processors.Abstractions
{
    /// <summary>
    /// Defines the contract for the processor.
    /// </summary>
    public interface IProcessor<T>
    {
        T Process(T value);
        IProcessor<T> Clone();
    }
}
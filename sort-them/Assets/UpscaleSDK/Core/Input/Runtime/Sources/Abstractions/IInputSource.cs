using UpscaleSDK.Core.Input.Provider.Abstractions;

namespace UpscaleSDK.Core.Input.Sources.Abstractions
{
    /// <summary>
    /// Defines the contract for the input source.
    /// </summary>
    public interface IInputSource<T>
    {
        T Accumulate(T current, IInputProvider provider, int player);
    }
}
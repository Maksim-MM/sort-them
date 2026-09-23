namespace UpscaleSDK.Core.Input.Core
{
    /// <summary>
    /// Represents a action result struct.
    /// </summary>
    public readonly struct ActionResult<T>
    {
        private readonly InputAction<T> _proxy;

        public ActionResult(InputAction<T> proxy)
        {
            _proxy = proxy;
        }
    
        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        public T Value => _proxy.Read(); 
    }
}
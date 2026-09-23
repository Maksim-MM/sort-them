namespace UpscaleSDK.Core.Saves.ErrorHandling
{
    /// <summary>
    /// Represents a error handler class.
    /// </summary>
    public static class ErrorHandler
    {
        /// <summary>
        /// Occurs when error.
        /// </summary>
        public static event OnErrorDelegate OnError;
        
        /// <summary>
        /// Handles the error delegate.
        /// </summary>
        /// <param name="errorMessage">The error message.</param>
        public delegate void OnErrorDelegate(string errorMessage);
        /// <summary>
        /// Raise error.
        /// </summary>
        /// <param name="errorMessage">The error message.</param>
        public static void RaiseError(string errorMessage) => OnError?.Invoke(errorMessage);
    }
}
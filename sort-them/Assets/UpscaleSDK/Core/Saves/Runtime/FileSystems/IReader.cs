using System;

namespace UpscaleSDK.Core.Saves.FileSystems
{
    /// <summary>
    /// Interface for a file reader.
    /// </summary>
    public interface IReader
    {
        /// <summary>
        /// Event triggered when an error occurs during a read operation. Error code may be presented as an integer or string.
        /// </summary>
        public event Action<int, string> OnReadError;
        
        /// <summary>
        /// Event triggered when a file read operation is finished.
        /// </summary>
        public event Action<string> OnFileReadFinished;
        
        /// <summary>
        /// Reads a file from the file system.
        /// </summary>
        /// <param name="fileName">Name of the file to read.</param>
        /// <param name="extension">Extension of the file to read.</param>
        /// <returns>True if the file was read successfully, false otherwise.</returns>
        public bool Read(string fileName, string extension);
    }
}
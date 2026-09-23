using System;

namespace UpscaleSDK.Core.Saves.FileSystems
{
    /// <summary>
    /// Interface for writing files to a file system.
    /// </summary>
    public interface IWriter
    {
        /// <summary>
        /// Event triggered when an error occurs during a write operation. Error code may be presented as an integer or string.
        /// </summary>
        public event Action<int, string> OnWriteError;
        
        /// <summary>
        /// Writes data to a file with the specified name and extension.
        /// </summary>
        /// <param name="fileName">Name of the file to write to.</param>
        /// <param name="extension">File extension of the file.</param>
        /// <param name="data">Data to write to the file.</param>
        public void Write(string fileName, string extension, string data);
    }
}
using System;

namespace UpscaleSDK.Core.Saves.FileSystems
{
    /// <summary>
    /// Interface for a file system that supports reading and writing files.
    /// </summary>
    public interface IFileSystem : IWriter, IReader
    {
        /// <summary>
        /// Event triggered when files are found during a search.
        /// </summary>
        public event Action<FileEntry[]> OnFilesFound;
        
        /// <summary>
        /// Starts searching for files in the file system.
        /// </summary>
        public void StartFileSearch();
    }
}
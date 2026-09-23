using System;

namespace UpscaleSDK.Core.Saves.FileSystems
{
    /// <summary>
    /// Represents a file entry struct.
    /// </summary>
    public struct FileEntry
    {
        /// <summary>
        /// The name field.
        /// </summary>
        public string Name;
        /// <summary>
        /// The extension field.
        /// </summary>
        public string Extension;
        /// <summary>
        /// The is directory field.
        /// </summary>
        public bool IsDirectory;
        /// <summary>
        /// The is save field.
        /// </summary>
        public bool IsSave;
        /// <summary>
        /// The last modified field.
        /// </summary>
        public DateTime? LastModified;
    }
}
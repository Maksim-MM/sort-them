using System;
using System.Collections.Generic;
using UpscaleSDK.Core.Saves.FileSystems;

namespace UpscaleSDK.Core.Saves
{
    /// <summary>
    /// Defines the contract for the saver events.
    /// </summary>
    public interface ISaverEvents
    {
        /// <summary>
        /// Occurs when save started.
        /// </summary>
        public event Action<Dictionary<string, string>> OnSaveStarted;
        /// <summary>
        /// Occurs when save completed.
        /// </summary>
        public event Action<string> OnSaveCompleted;
        /// <summary>
        /// Occurs when load started.
        /// </summary>
        public event Action OnLoadStarted;
        /// <summary>
        /// Occurs when load completed.
        /// </summary>
        public event Action<bool> OnLoadCompleted;
        /// <summary>
        /// Occurs when saves found.
        /// </summary>
        public event Action<FileEntry[]> OnSavesFound;

        /// <summary>
        /// Call on save started.
        /// </summary>
        /// <param name="saveData">The save data.</param>
        public void CallOnSaveStarted(Dictionary<string, string> saveData);
        /// <summary>
        /// Call on save completed.
        /// </summary>
        /// <param name="fileName">The file name.</param>
        public void CallOnSaveCompleted(string fileName);
        /// <summary>
        /// Call on load started.
        /// </summary>
        public void CallOnLoadStarted();
        /// <summary>
        /// Call on load completed.
        /// </summary>
        /// <param name="result">The result.</param>
        public void CallOnLoadCompleted(bool result);
        /// <summary>
        /// Call on saves found.
        /// </summary>
        /// <param name="entries">The entries.</param>
        public void CallOnSavesFound(FileEntry[] entries);
    }
}
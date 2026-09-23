using System;
using UnityEngine;
using UpscaleSDK.Core.Saves.FileSystems;

namespace UpscaleSDK.Core
{
    /// <summary>
    /// Represents a platform class.
    /// </summary>
    public abstract class Platform : MonoBehaviour
    {
        /// <summary>
        /// Occurs when saves initialized.
        /// </summary>
        public event Action OnSavesInitialized;
        /// <summary>
        /// Occurs when context request save.
        /// </summary>
        public event Action OnContextRequestSave;

        /// <summary>
        /// Invoke on saves initialized.
        /// </summary>
        protected void InvokeOnSavesInitialized()
        {
            OnSavesInitialized?.Invoke();
        }

        /// <summary>
        /// Invoke on context request save.
        /// </summary>
        protected void InvokeOnContextRequestSave()
        {
            OnContextRequestSave?.Invoke();
        }

        /// <summary>
        /// Initialize.
        /// </summary>
        public abstract void Initialize();
        /// <summary>
        /// Provide file system.
        /// </summary>
        public abstract IFileSystem ProvideFileSystem();
    }
}

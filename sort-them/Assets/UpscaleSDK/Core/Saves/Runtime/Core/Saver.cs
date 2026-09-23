using System;
using System.Collections.Generic;
using UpscaleSDK.Core.Saves.FileSystems;
using UpscaleSDK.Core.Saves.Serialization;
using UpscaleSDK.Core.Utils;

namespace UpscaleSDK.Core.Saves
{
    /// <summary>
    /// Represents a saver class.
    /// </summary>
    public class Saver : ISaverEvents
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
        /// Occurs when read error.
        /// </summary>
        public event Action<int, string> OnReadError;
        /// <summary>
        /// Occurs when write error.
        /// </summary>
        public event Action<int, string> OnWriteError;
        
        public Dictionary<string, string> CurrentSaveState
        {
            get => _currentSaveState;
            set => _currentSaveState = value;
        }

        private Dictionary<string, string> _currentSaveState;
        private string _fileExtension;
        private ISerializator _serializator;
        private IFileSystem _fileSystem;

        public Saver(string fileExtension, ISerializator serializator, IFileSystem fileSystem)
        {
            _currentSaveState = new Dictionary<string, string>();
            _serializator = serializator;
            _fileExtension = fileExtension;
            _fileSystem = fileSystem;
            _fileSystem.OnReadError += OnFileSystemReadError;
            _fileSystem.OnWriteError += OnFileSystemWriteError;
        }
        
        /// <summary>
        /// Saves the current state to a file.
        /// </summary>
        /// <param name="saveName">File name</param>
        /// <param name="isAutosave">Is save an autosave</param>
        public void Save(string saveName = null, bool isAutosave = false)
        {
            var fileName = string.IsNullOrEmpty(saveName) ? $"Save_{DateTime.Now:yyyyMMdd_HHmmss}" : saveName;

            OnSaveStarted?.Invoke(_currentSaveState);
            SerializableSave save = new SerializableSave()
            {
                Name = fileName,
                IsAutoSave = isAutosave,
                Timestamp = DateTime.Now,
                Data = _currentSaveState
            };
            _fileSystem.Write(fileName, _fileExtension, _serializator.Serialize(save));
            OnSaveCompleted?.Invoke(fileName);
        }

        /// <summary>
        /// Loads the state from a file.
        /// </summary>
        /// <param name="saveName">File name</param>
        public void Load(string saveName)
        {
            OnLoadStarted?.Invoke();
            _fileSystem.OnFileReadFinished += OnFileSystemFileReadFinished;
            bool manualResult = _fileSystem.Read(saveName, _fileExtension);
            if (!manualResult)
            {
                UPSLogger.SavesInfoLog($"Save file '{saveName}' not found in directories.");
                OnLoadCompleted?.Invoke(false);
            }
        }

        /// <summary>
        /// Starts searching for save files. When found, invokes OnSavesFound event.
        /// </summary>
        public void StartSavesSearch()
        {
            _fileSystem.OnFilesFound += OnFilesFound;
            _fileSystem.StartFileSearch();
        }

        /// <summary>
        /// Call on save started.
        /// </summary>
        /// <param name="saveData">The save data.</param>
        public void CallOnSaveStarted(Dictionary<string, string> saveData) => OnSaveStarted?.Invoke(saveData);

        /// <summary>
        /// Call on save completed.
        /// </summary>
        /// <param name="fileName">The file name.</param>
        public void CallOnSaveCompleted(string fileName) => OnSaveCompleted?.Invoke(fileName);

        /// <summary>
        /// Call on load started.
        /// </summary>
        public void CallOnLoadStarted() => OnLoadStarted?.Invoke();

        /// <summary>
        /// Call on load completed.
        /// </summary>
        /// <param name="result">The result.</param>
        public void CallOnLoadCompleted(bool result) => OnLoadCompleted?.Invoke(result);
        /// <summary>
        /// Call on saves found.
        /// </summary>
        /// <param name="entries">The entries.</param>
        public void CallOnSavesFound(FileEntry[] entries) => OnSavesFound?.Invoke(entries);

        private void OnFileSystemFileReadFinished(string data)
        {
            _fileSystem.OnFileReadFinished -= OnFileSystemFileReadFinished;
            SerializableSave save = _serializator.Deserialize<SerializableSave>(data);
            if (save.Data != null)
            {
                _currentSaveState = save.Data;
                OnLoadCompleted?.Invoke(true);
            }
            else
            {
                UPSLogger.SavesErrorLog("Failed to deserialize save data.");
                OnLoadCompleted?.Invoke(false);
            }
        }
        
        private void OnFilesFound(FileEntry[] obj)
        {
            OnSavesFound?.Invoke(obj);
            _fileSystem.OnFilesFound -= OnFilesFound;
        }
        
        private void OnFileSystemWriteError(int arg1, string arg2) => OnWriteError?.Invoke(arg1, arg2);

        private void OnFileSystemReadError(int arg1, string arg2) => OnReadError?.Invoke(arg1, arg2);
    }
}
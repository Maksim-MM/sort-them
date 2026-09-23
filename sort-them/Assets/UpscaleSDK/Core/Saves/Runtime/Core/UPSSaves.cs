using System;
using UnityEngine;
using UpscaleSDK.Core.Configuration;
using UpscaleSDK.Core.Saves.FileSystems;
using UpscaleSDK.Core.Saves.Serialization;
using UpscaleSDK.Core.Utils;

namespace UpscaleSDK.Core.Saves
{
    /// <summary>
    /// Central manager for the save system. Handles initialization and provides access to save functionalities.
    /// </summary>
    public class UPSSaves : MonoBehaviour
    {
        private IFileSystem _fileSystem;
        private ISerializator _serializator;
        private Saver _saver;

        /// <summary>
        /// Occurs when is ready.
        /// </summary>
        public static event Action IsReady;
        private Platform _platform;
        private UPSPlayerPrefs _prefs;
        private static UPSSaves _instance;

        /// <summary>
        /// Gets or sets the prefs.
        /// </summary>
        public static UPSPlayerPrefs Prefs => _instance._prefs;
        /// <summary>
        /// Gets or sets the saver.
        /// </summary>
        public static Saver Saver => _instance._saver;
        
        /// <summary>
        /// Initialize.
        /// </summary>
        /// <param name="platform">The platform.</param>
        public void Initialize(Platform platform)
        {
            _instance = this;
            _platform = platform;

            _platform.OnSavesInitialized += OnSavesInitialized;
            _platform.OnContextRequestSave += OnContextRequestSave;
            _fileSystem = platform.ProvideFileSystem();
            _serializator = new SerializatorFactory().Create();
            _saver = new Saver(ConfigurationProvider.GetConfiguration().GetSavesSettings().SaveFileExtension, _serializator, _fileSystem);
          
            _prefs = new UPSPlayerPrefs();
        }

        private void OnContextRequestSave()
        {
            _prefs.TrySave();
        }

        private void OnSavesInitialized()
        {
            UPSLogger.SavesInfoLog("Initializing SaveManager");
            
            _prefs.Initialize(_serializator, _saver, ConfigurationProvider.GetConfiguration().GetSavesSettings());

            _prefs.Update();
            IsReady?.Invoke();
            UPSLogger.SavesInfoLog("Initialized SaveManager");
        }
        
        private void Update()
        {
            _prefs.Update();
        }
    }
}
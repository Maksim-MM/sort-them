using System;
using System.Collections.Generic;
using UnityEngine;
using UpscaleSDK.Core.Saves.FileSystems;
using UpscaleSDK.Core.Saves.Serialization;
using UpscaleSDK.Core.Saves.Settings;
using UpscaleSDK.Core.Utils;

namespace UpscaleSDK.Core.Saves
{
    /// <summary>
    /// Represents a ups player prefs class.
    /// </summary>
    public class UPSPlayerPrefs
    {
        /// <summary>
        /// The is ready field.
        /// </summary>
        public bool IsReady = false;
        /// <summary>
        /// Occurs when ready.
        /// </summary>
        public event Action OnReady;
        private Dictionary<string, string> SaveData => _saver.CurrentSaveState;
        private Saver _saver;
        private ISerializator _serializator;
        private SavesFoundState _filesState = SavesFoundState.None;
        private bool _loadingStarted;
        private SavesSettings _savesSettings;
        private AutoSaver _autoSaver;

        internal void Initialize(ISerializator serializator, Saver saver, SavesSettings savesSettings)
        {
            _savesSettings = savesSettings;
            _serializator = serializator;
            _saver = saver;
            _saver.OnSavesFound += OnSavesFound;
            _saver.OnReadError += OnReadError;
            _saver.StartSavesSearch();
            _autoSaver = new AutoSaver(savesSettings.EnableAutosaves, savesSettings.SecondsBetweenAutosaves);
            _autoSaver.OnAutoSaveStarted += OnAutoSave;
        }

        private void OnAutoSave()
        {
            UPSLogger.SavesDevLog("Auto save attempt");
            TrySave();
        }

        internal void Update()
        {
            _autoSaver.Tick(Time.unscaledDeltaTime);
            
            if (IsReady) 
                return;
            if (_filesState == SavesFoundState.None) 
                return;
            if (_filesState == SavesFoundState.SaveExist)
            {
                if (_loadingStarted == false)
                    LoadSave();
            }
            else
            {
                InitiateNewSaveData();
            }
        }

        private void OnReadError(int arg1, string arg2)
        {
            UPSLogger.SavesInfoLog("Read error");
            InitiateNewSaveData();
        }

        private void OnSavesFound(FileEntry[] obj)
        {
            foreach (FileEntry file in obj)
            {
                if (file.Name == _savesSettings.XboxContainerName)
                {
                    _filesState = SavesFoundState.SaveExist;
                    return;
                }
            }
            _filesState = SavesFoundState.NoSaves;
        }

        private void InitiateNewSaveData()
        {
            IsReady = true;
            OnReady?.Invoke();
        }

        private void LoadSave()
        {
            _loadingStarted = true;
            _saver.OnLoadCompleted += LoadCompleted;
            _saver.Load(_savesSettings.XboxContainerName);
        }

        private void LoadCompleted(bool obj)
        {
            _saver.OnLoadCompleted -= LoadCompleted;
            IsReady = true;
            OnReady?.Invoke();
        }

        /// <summary>
        /// Sets the int.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public void SetInt(string key, int value)
        {
            _saver.CurrentSaveState[key] = _serializator.Serialize(value);
        }

        /// <summary>
        /// Gets the int.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="defaultValue">The default value.</param>
        public int GetInt(string key, int defaultValue = 0)
        {
            if (SaveData.TryGetValue(key, out var serializedValue))
            {
                return _serializator.Deserialize<int>(serializedValue);
            }

            return defaultValue;
        }

        /// <summary>
        /// Sets the string.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public void SetString(string key, string value)
        {
            _saver.CurrentSaveState[key] = _serializator.Serialize(value);
        }

        /// <summary>
        /// Gets the string.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="defaultValue">The default value.</param>
        public string GetString(string key, string defaultValue = "")
        {
            if (SaveData.TryGetValue(key, out var serializedValue))
            {
                return _serializator.Deserialize<string>(serializedValue);
            }

            return defaultValue;
        }

        /// <summary>
        /// Sets the float.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public void SetFloat(string key, float value)
        {
            _saver.CurrentSaveState[key] = _serializator.Serialize(value);
        }

        /// <summary>
        /// Gets the float.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="defaultValue">The default value.</param>
        public float GetFloat(string key, float defaultValue = 0f)
        {
            if (SaveData.TryGetValue(key, out var serializedValue))
            {
                return _serializator.Deserialize<float>(serializedValue);
            }

            return defaultValue;
        }
        
        /// <summary>
        /// Sets the bool.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public void SetBool(string key, bool value)
        {
            _saver.CurrentSaveState[key] = _serializator.Serialize(value);
        }

        /// <summary>
        /// Gets the bool.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="defaultValue">The default value.</param>
        public bool GetBool(string key, bool defaultValue = false)
        {
            if (SaveData.TryGetValue(key, out var serializedValue))
            {
                return _serializator.Deserialize<bool>(serializedValue);
            }

            return defaultValue;
        }

        /// <summary>
        /// Indicates whether the instance has key.
        /// </summary>
        /// <param name="key">The key.</param>
        public bool HasKey(string key)
        {
            return SaveData.ContainsKey(key);
        }

        /// <summary>
        /// Delete key.
        /// </summary>
        /// <param name="key">The key.</param>
        public void DeleteKey(string key)
        {
            if (SaveData.ContainsKey(key))
            {
                SaveData.Remove(key);
            }
        }

        /// <summary>
        /// Attempts to save.
        /// </summary>
        public void TrySave()
        {
            if (IsReady == false)
                return;

            _saver.Save(_savesSettings.XboxContainerName);
        }
        
        private enum SavesFoundState
        {
            /// <summary>
            /// None.
            /// </summary>
            None,
            /// <summary>
            /// Save exist.
            /// </summary>
            SaveExist,
            /// <summary>
            /// No saves.
            /// </summary>
            NoSaves
        }
    }
}
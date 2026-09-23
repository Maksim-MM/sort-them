using System;
using Plugins.UpscaleSDK.PS4.Runtime.App;
using Plugins.UpscaleSDK.Saves.Runtime.PS4;
using UnityEngine;
using UpscaleSDK.Core;
using UpscaleSDK.Core.Configuration;
using UpscaleSDK.Core.Saves.FileSystems;
using UpscaleSDK.Core.Utils;


#if UNITY_PS4
using Sony.PS4.SaveData;
#endif

namespace Plugins.UpscaleSDK.PS4.Runtime.Common
{
    /// <summary>
    /// Represents a ps4 platform class.
    /// </summary>
    public class PS4Platform : Platform
    {
        /// <summary>
        /// The on initialized field.
        /// </summary>
        public Action OnInitialized;
        /// <summary>
        /// Gets or sets the is initialized.
        /// </summary>
        public bool IsInitialized => _appInitializator.IsInitialized;
        
        private AppInitializator _appInitializator;
        private MainUpdater _mainUpdater;
        private bool _sentInitializedEvent = false;
        
#if !UNITY_EDITOR && UNITY_PS4
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        private static void RegisterPlatform()
        {
            PlatformProvider.SetPlatform<PS4Platform>();
        }
#endif
        
        
#if UNITY_PS4
        private InitResult _initResult;
        private Mounting.MountPoint _mountPoint;

        internal event Action<SaveDataCallbackEvent> OnSaveDataEvent;
#endif

        /// <summary>
        /// Initialize saves.
        /// </summary>
        public void InitializeSaves()
        {
            Debug.Log("BEGIN INITIALIZE PS4 SAVES");
#if UNITY_PS4
            Main.OnAsyncEvent += OnAsyncEvent;
            try
            {
                InitSettings initSettings = new InitSettings();
                initSettings.Affinity = ThreadAffinity.Core5;
                _initResult = Main.Initialize(initSettings);
            }
            catch (SaveDataException ex)
            {
                Debug.LogError($"PS4 Save Data Initialization failed with error: {ex.ExtendedMessage}");
            }
#if UNITY_EDITOR
            catch (DllNotFoundException e)
            {
                Debug.LogWarning($"Dll not found: {e.Message}");
                Debug.LogWarning("PS4 Save Data plugin is not supported in the Editor");
            }
#endif
#endif
            Debug.Log("END INITIALIZE PS4 SAVES");
        }

        /// <summary>
        /// Terminate saves.
        /// </summary>
        public void TerminateSaves()
        {
#if UNITY_PS4
            try
            {
                Main.Terminate();
                _initResult = new();
            }
            catch (SaveDataException ex)
            {
                Debug.LogError($"Error during termination of PS4 Save Data: {ex.ExtendedMessage}");
            }
#endif
        }

#if UNITY_PS4
        private void OnAsyncEvent(SaveDataCallbackEvent npEvent)
        {
            OnSaveDataEvent?.Invoke(npEvent);
        }
#endif
#if UNITY_PS4
        private void Update()
        {
            if (_appInitializator.IsInitialized && _sentInitializedEvent == false && _initResult.Initialized)
            {
                _sentInitializedEvent = true;
                OnInitialized?.Invoke();
                InvokeOnSavesInitialized();
                UnityEngine.PS4.Utility.onSystemServiceFlagEvent += OnFlagChnageState;
            }
        }

        private void OnFlagChnageState(UnityEngine.PS4.Utility.SystemServiceFlag flagindex, bool newValue)
        {
            if (flagindex == UnityEngine.PS4.Utility.SystemServiceFlag.SystemUiOverlaid)
            {
                if(newValue)
                {
                    InvokeOnContextRequestSave();
                }
            }
        }

#endif
        /// <summary>
        /// Initialize.
        /// </summary>
        public override void Initialize()
        {
            Debug.Log("BEGIN INTIALIZE PS4");

            if (_mainUpdater == null)
            {
                _mainUpdater = gameObject.AddComponent<MainUpdater>();
            }

            if (_appInitializator == null)
            {
                _appInitializator = gameObject.AddComponent<AppInitializator>();
            }
            _appInitializator.TryInitialize();

           
            Debug.Log("END INTIALIZE PS4");

            InitializeSaves();
        }

        /// <summary>
        /// Provide file system.
        /// </summary>
        public override IFileSystem ProvideFileSystem()
        {
            return new PS4FileSystem(this, ConfigurationProvider.GetConfiguration().GetSavesSettings());
        }

        private void OnDestroy()
        {
            InvokeOnContextRequestSave();
            TerminateSaves();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus == false)
            {
                Debug.Log("FOCUS");
                InvokeOnContextRequestSave();
            }
        }

        private void OnApplicationQuit()
        {
            InvokeOnContextRequestSave();
        }
    }
}
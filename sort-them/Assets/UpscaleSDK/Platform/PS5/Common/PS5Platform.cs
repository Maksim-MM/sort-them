using System;
using Plugins.UpscaleSDK.PS5.Runtime.App;
using Plugins.UpscaleSDK.PS5.Runtime.Users;
using Plugins.UpscaleSDK.Saves.Runtime.PS5;
#if UNITY_PS5
using Unity.SaveData.PS5;
using Unity.SaveData.PS5.Core;
using Unity.SaveData.PS5.Initialization;
using Unity.SaveData.PS5.Mount;
#endif
using UnityEngine;
using UpscaleSDK.Core;
using UpscaleSDK.Core.Configuration;
using UpscaleSDK.Core.Saves.FileSystems;

namespace Plugins.UpscaleSDK.PS5.Runtime.Common
{

    /// <summary>
    /// Represents a ps5 platform class.
    /// </summary>
    public class PS5Platform : Platform
    {
        /// <summary>
        /// Occurs when initialized.
        /// </summary>
        public event Action OnInitialized;
        /// <summary>
        /// Gets or sets the is initialized.
        /// </summary>
        public bool IsInitialized => _appInitializer.IsInitialized && _userInitializer.IsInitialized;
        
        private AppInitializer _appInitializer;
        private UserInitializer _userInitializer;
        private bool _sentInitializedEvent = false;
        
        
#if !UNITY_EDITOR && UNITY_PS5
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        private static void RegisterPlatform()
        {
            PlatformProvider.SetPlatform<PS5Platform>();
        }
#endif
#if UNITY_PS5     
        private void Update()
        {
            if (IsInitialized && _sentInitializedEvent == false && _initResult.Initialized)
            {
                _sentInitializedEvent = true;
                OnInitialized?.Invoke();
                InvokeOnSavesInitialized(); ///////////////////////////////
            }
        }
#endif
        /// <summary>
        /// Initialize.
        /// </summary>
        public override void Initialize()
        {
            Debug.Log("[PS5RUNTIME] START INIT");

            gameObject.AddComponent<MainThreadDispatcher>();

            if (_appInitializer == null)
            {
                _appInitializer = gameObject.AddComponent<AppInitializer>();
            }

            if (_userInitializer == null)
            {
                _userInitializer = gameObject.AddComponent<UserInitializer>();
            }

            try
            {
                _appInitializer.TryInitialize();
            }
            catch (Exception ex)
            {
                Debug.Log("App Initialization Failed: " + ex);
            }

            try
            {
                _userInitializer.TryInitialize();
            }
            catch (Exception e)
            {
                Debug.Log("User Initialization Failed: " + e);
            }
            Debug.Log("[PS5RUNTIME] END INIT");
            InitSaves();
        }
        
#if UNITY_PS5
        private InitResult _initResult;
        private Mounting.MountPoint _mountPoint;

        internal event Action<SaveDataCallbackEvent> OnSaveDataEvent; 
#endif

        /// <summary>
        /// Init saves.
        /// </summary>
        public void InitSaves()
        {
            Debug.Log("[PS5SAVES] START INIT");
#if UNITY_PS5
            Main.OnAsyncEvent += OnAsyncEvent;
            try
            {
                InitSettings initSettings = new InitSettings();
                initSettings.Affinity = ThreadAffinity.Core5;
                _initResult = Main.Initialize(initSettings);
            }
            catch (SaveDataException ex)
            {
                Debug.LogError($"PS5 Save Data Initialization failed with error: {ex.ExtendedMessage}");
            }
#if UNITY_EDITOR
            catch (DllNotFoundException e)
            {
                Debug.LogWarning($"Dll not found: {e.Message}");
                Debug.LogWarning("PS5 Save Data plugin is not supported in the Editor");
            }
#endif
#endif
            Debug.Log("[PS5RUNTIME] END INIT");
        }


        /// <summary>
        /// Terminate saves.
        /// </summary>
        public void TerminateSaves()
        {
#if UNITY_PS5
            try
            {
                Main.Terminate();
                _initResult = new();
            }
            catch (SaveDataException ex)
            {
                Debug.LogError($"Error during termination of PS5 Save Data: {ex.ExtendedMessage}");
            }
#endif
        }

#if UNITY_PS5
        private void OnAsyncEvent(SaveDataCallbackEvent npEvent)
        {
            Debug.Log($"API CALLED ON ASYNC EVENT: {npEvent.ApiCalled} REQUEST ID: {npEvent.RequestId} USER ID: {npEvent.UserId}");
            //OnSaveDataEvent?.Invoke(npEvent);
        }
#endif

        /// <summary>
        /// Provide file system.
        /// </summary>
        public override IFileSystem ProvideFileSystem()
        {
            return new PS5FileSystem(this, ConfigurationProvider.GetConfiguration().GetSavesSettings());
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
                InvokeOnContextRequestSave();
            }
        }
    }
}
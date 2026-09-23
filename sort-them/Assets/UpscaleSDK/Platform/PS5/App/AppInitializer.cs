using System;
using Plugins.UpscaleSDK.PS5.Runtime.DependencyManagement;
using UnityEngine;
#if UNITY_PS5
using UnityEngine.PS5;
#endif

namespace Plugins.UpscaleSDK.PS5.Runtime.App
{
    /// <summary>
    /// Represents a app initializer class.
    /// </summary>
    public class AppInitializer : MonoBehaviour
    {
        /// <summary>
        /// Gets or sets the app data.
        /// </summary>
        public AppData AppData => _appData;
        /// <summary>
        /// Gets or sets the is initialized.
        /// </summary>
        public bool IsInitialized => _initialized;
        
        private AppData _appData;
        private bool _initialized = false;
        
        /// <summary>
        /// Attempts to initialize.
        /// </summary>
        public void TryInitialize()
        {
            if (_initialized) return;

            Initialize();
        }

        private void Initialize()
        {
            _appData = new();
            IDependency<AppData>.Instance = _appData;
#if UNITY_PS5
            AppData.ContentId = Utility.contentId;
            AppData.IsDemo = Utility.GetApplicationParameter(1) == 1;
            AppData.IsEditor = Application.isEditor;
            AppData.CanUseSocial = true;
            if (AppData.IsEditor) AppData.CanUseSocial = false;
            if (AppData.ContentId == "ED5843-NPXX53349_00-XXXXXXXXXXXXXXXX" || string.IsNullOrEmpty(_appData.ContentId)) AppData.CanUseSocial = false;
            if (_appData.IsDemo) AppData.CanUseSocial = false;
            _initialized = true;
#endif
        }
    }
}
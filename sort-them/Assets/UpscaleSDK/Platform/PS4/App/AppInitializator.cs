using Plugins.UpscaleSDK.PS4.Runtime.DependencyManagement;
using Plugins.UpscaleSDK.PS4.Runtime.TrophiesManagement;
using Plugins.UpscaleSDK.PS4.Runtime.User;
using UnityEngine;
#if UNITY_PS4
using Sony.NP;
using UnityEngine.PS4;
#endif

namespace Plugins.UpscaleSDK.PS4.Runtime.App
{
    /// <summary>
    /// Represents a app initializator class.
    /// </summary>
    public class AppInitializator : MonoBehaviour
    {
        /// <summary>
        /// Gets or sets the app data.
        /// </summary>
        public AppData AppData => _appdata;
        /// <summary>
        /// Gets or sets the user data.
        /// </summary>
        public UserData UserData => _userData;
        /// <summary>
        /// Gets or sets the is initialized.
        /// </summary>
        public bool IsInitialized => _isInitialized;
        
        private AppData _appdata;
        private UserData _userData;
        private Ps4Trophies _trophiesHandler;
        private bool _isInitialized = false;
        
        /// <summary>
        /// Attempts to initialize.
        /// </summary>
        public void TryInitialize()
        {
            if (_isInitialized)
                return;

            Initialize();
        }

        private void Initialize()
        {
            _appdata = new();
            _userData = new();
            _trophiesHandler = new Ps4Trophies(_appdata, _userData);
#if UNITY_PS4
            _appdata.NpTitleId = Utility.npTitleId;
            _appdata.IsDemoApplication = Utility.GetApplicationParameter(1) == 1;
            _appdata.CanUseSocial = true;

            IDependency<UserData>.Instance = _userData;
            IDependency<AppData>.Instance = _appdata;
            IDependency<Ps4Trophies>.Instance = _trophiesHandler;
            
            if (Application.isEditor) _appdata.CanUseSocial = false;
            if (_appdata.NpTitleId == "NPXX51362_00" || string.IsNullOrEmpty(_appdata.NpTitleId)) _appdata.CanUseSocial = false;
            if (_appdata.IsDemoApplication) _appdata.CanUseSocial = false;

            _userData.PS4User = PS4Input.GetUsersDetails(0);
            InitToolkit initToolkit = new();
            initToolkit.SetPushNotificationsFlags(PushNotificationsFlags.None);
            try
            {
                InitResult result = Main.Initialize(initToolkit);
                
                if (result.Initialized)
                {
                    if (AppData.CanUseSocial)
                    {
                        RegisterTrophyPack();
                    }
                  
                    _isInitialized = true;
                }
            }
            catch (NpToolkitException ex)
            {
                Debug.LogError($"PlayStation Network initialization error: {ex.Message}");
            }
#endif
        }

        private void RegisterTrophyPack()
        {
#if UNITY_PS4
            try
            {
                Trophies.RegisterTrophyPackRequest request = new()
                {
                    UserId = _userData.PS4User.userId
                };

                Core.EmptyResponse response = new();

                int requestId = Trophies.RegisterTrophyPack(request, response);
            }
            catch (NpToolkitException ex)
            {
                Debug.LogError($"Failed to register trophy pack: {ex.Message}");
            }
#endif
        }
    }
}
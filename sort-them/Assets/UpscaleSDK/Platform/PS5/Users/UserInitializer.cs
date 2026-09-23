using Plugins.UpscaleSDK.PS5.Runtime.App;
using Plugins.UpscaleSDK.PS5.Runtime.Common;
using Plugins.UpscaleSDK.PS5.Runtime.DependencyManagement;
using Plugins.UpscaleSDK.PS5.Runtime.Users.Activities;
using Plugins.UpscaleSDK.PS5.Runtime.Users.Trophies;
using UnityEngine;
#if UNITY_PS5
using System;
using Unity.PSN.PS5;
using Unity.PSN.PS5.Aysnc;
using Unity.PSN.PS5.GameIntent;
using Unity.PSN.PS5.Initialization;
using Unity.PSN.PS5.Sessions;
using Unity.PSN.PS5.UDS;
using Unity.PSN.PS5.Users;
using Unity.PSN.PS5.Trophies;
using UnityEngine.PS5;
#endif

namespace Plugins.UpscaleSDK.PS5.Runtime.Users
{
    /// <summary>
    /// Represents a user initializer class.
    /// </summary>
    public class UserInitializer : MonoBehaviour
    {
        /// <summary>
        /// Gets or sets the user data.
        /// </summary>
        public UserData UserData => _userData;
        /// <summary>
        /// Gets or sets the trophies data.
        /// </summary>
        public TrophiesData TrophiesData => _trophiesData;
        /// <summary>
        /// Gets or sets the ps5 trophies.
        /// </summary>
        public Ps5Trophies Ps5Trophies => _ps5Trophies;
        /// <summary>
        /// Gets or sets the ps5 activities.
        /// </summary>
        public Ps5Activities Ps5Activities => _ps5Activities;
        /// <summary>
        /// Gets or sets the is initialized.
        /// </summary>
        public bool IsInitialized => _initialized;
        
        private UserData _userData;
        private TrophiesData _trophiesData;
        private AppData _appData;
        private Ps5Trophies _ps5Trophies;
        private Ps5Activities _ps5Activities;
        private bool _initialized = false;

        /// <summary>
        /// Attempts to initialize.
        /// </summary>
        public void TryInitialize()
        {
            if (_initialized) return;

            _appData = IDependency<AppData>.Instance;
            Initialize();
        }

        private void Initialize()
        {
            _userData = new();
            _trophiesData = new TrophiesData();
#if UNITY_PS5
            _trophiesData.AllTrophies = new();
#endif
            _ps5Trophies = new(_appData, _userData, _trophiesData);
            _ps5Activities = new(_appData, _userData);
            IDependency<UserData>.Instance = _userData;
            IDependency<TrophiesData>.Instance = _trophiesData;
            IDependency<Ps5Trophies>.Instance = _ps5Trophies;
            IDependency<Ps5Activities>.Instance = _ps5Activities;
#if UNITY_PS5
            UserData.PSUser = PS5Input.GetUsersDetails(0);
            PS5Input.OnUserServiceEvent += OnUserServiceEvent;
            InitResult initResult = Main.Initialize();

            if (initResult.Initialized)
            {
                GameIntentSystem.OnGameIntentNotification += OnGameIntentNotification;
                InitializeUniversalDataSystem();
                InitializeTrophySystem();
                _initialized = true;

                _ps5Trophies.OnTrophyUnlocked += (trophyId) => { ParseTrophy(trophyId, null, true); };
            }
            else
            {
                Debug.Log("PSN Initialization Failed");
            }
#endif
        }

        private void InitializeUniversalDataSystem()
        {
#if UNITY_PS5
            UniversalDataSystem.StartSystemRequest request = new()
            {
                PoolSize = 256 * 1024
            };

            var requestOperation = new AsyncRequest<UniversalDataSystem.StartSystemRequest>(request).ContinueWith((
                antecedent) =>
            {
                if (antecedent.IsSuccess())
                {
                }
            });

            UniversalDataSystem.Schedule(requestOperation);
#endif
        }

        private void InitializeTrophySystem()
        {
#if UNITY_PS5

            TrophySystem.StartSystemRequest request = new();
            var requestOperation = new AsyncRequest<TrophySystem.StartSystemRequest>(request).ContinueWith((
                antecedent) =>
            {
            });

            TrophySystem.Schedule(requestOperation);
#endif
        }

#if UNITY_PS5
        private void OnUserServiceEvent(PS5Input.UserServiceEventType eventType, uint userId)
        {
            switch (eventType)
            {
                case PS5Input.UserServiceEventType.Login:
                    RegisterUserSession(userId);
                    break;

                case PS5Input.UserServiceEventType.Logout:
                    UnregisterUserSession(userId);
                    break;
            }
        }
#endif

        private void RegisterUserSession(uint userId)
        {
#if UNITY_PS5

            UserSystem.AddUserRequest request = new()
            {
                UserId = (int)userId
            };

            var requestOperation = new AsyncRequest<UserSystem.AddUserRequest>(request).ContinueWith((antecedent) =>
            {
                if (antecedent.IsSuccess())
                {
                    SessionsManager.RegisterUserSessionEvent((int)userId);
                    ParseTrophies((details, data) =>
                    {
                        for (int i = 0; i < details.NumTrophies; i++)
                        {
                            ParseTrophy(i);
                        }
                    });
                }
            });

            UserSystem.Schedule(requestOperation);
#endif
        }

        private void UnregisterUserSession(uint userId)
        {
#if UNITY_PS5
            SessionsManager.UnregisterUserSessionEventAsync((int)userId);

            UserSystem.RemoveUserRequest request = new()
            {
                UserId = (int)userId
            };

            var requestOperation = new AsyncRequest<UserSystem.RemoveUserRequest>(request).ContinueWith((antecedent) =>
            {
            });

            UserSystem.Schedule(requestOperation);
#endif
        }

#if UNITY_PS5
        private void ParseTrophies(Action<TrophySystem.TrophyGameDetails, TrophySystem.TrophyGameData> result = null,
            bool forceRefresh = false)
        {
            if (_appData == null) _appData = IDependency<AppData>.Instance;
            if (_appData.CanUseSocial == false) return;

            if (TrophiesData.GameDetails != null && TrophiesData.GameDetails != null && forceRefresh == false)
            {
                result?.Invoke(TrophiesData.GameDetails, TrophiesData.GameData);
                return;
            }

            TrophySystem.GetGameInfoRequest request = new()
            {
                UserId = UserData.PSUser.userId,
                GameDetails = new(),
                GameData = new()
            };

            var requestOperation =
                new AsyncRequest<TrophySystem.GetGameInfoRequest>(request).ContinueWith((antecedent) =>
                {
                    if (antecedent.IsSuccess())
                    {
                        TrophiesData.GameDetails = antecedent.Request.GameDetails;
                        TrophiesData.GameData = antecedent.Request.GameData;
                        result?.Invoke(TrophiesData.GameDetails, TrophiesData.GameData);
                    }
                });

            TrophySystem.Schedule(requestOperation);
        }
#endif

#if UNITY_PS5
        private void ParseTrophy(int trophyId,
            Action<TrophySystem.TrophyDetails, TrophySystem.TrophyData> result = null, bool forceRefresh = false)
        {
            if (_appData == null) _appData = IDependency<AppData>.Instance;
            if (_appData.CanUseSocial == false) return;

            if (TrophiesData.AllTrophies == null) TrophiesData.AllTrophies = new();
            if (TrophiesData.AllTrophies.ContainsKey(trophyId) && forceRefresh == false)
            {
                result?.Invoke(_trophiesData.AllTrophies[trophyId].Details, _trophiesData.AllTrophies[trophyId].Data);
                return;
            }

            TrophySystem.GetTrophyInfoRequest request = new()
            {
                UserId = UserData.PSUser.userId,
                TrophyId = trophyId,
                TrophyDetails = new(),
                TrophyData = new()
            };

            var requestOperation = new AsyncRequest<TrophySystem.GetTrophyInfoRequest>(request).ContinueWith((
                antecedent) =>
            {
                if (antecedent.IsSuccess())
                {
                    if (TrophiesData.AllTrophies == null) TrophiesData.AllTrophies = new();
                    if (TrophiesData.AllTrophies.ContainsKey(trophyId) == false)
                        TrophiesData.AllTrophies.Add(trophyId, new Trophy());

                    TrophiesData.AllTrophies[trophyId].Details = antecedent.Request.TrophyDetails;
                    TrophiesData.AllTrophies[trophyId].Data = antecedent.Request.TrophyData;
                    result?.Invoke(TrophiesData.AllTrophies[trophyId].Details, TrophiesData.AllTrophies[trophyId].Data);
                }
            });

            UniversalDataSystem.Schedule(requestOperation);
        }
#endif

#if UNITY_PS5
        private void OnGameIntentNotification(GameIntentSystem.GameIntent gameIntent)
        {
            Debug.Log($"Game Intent Received: {gameIntent.IntentType}");
        }
#endif

#if UNITY_PS5
        private void Update()
        {
            if (_appData != null && _appData.CanUseSocial)
                Main.Update();
        }
#endif
    }
}
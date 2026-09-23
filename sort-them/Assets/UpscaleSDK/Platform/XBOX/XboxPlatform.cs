#if UNITY_GAMECORE_XBOXSERIES || UNITY_GAMECORE_XBOXONE
#define SUPPORT_XBOX
#endif

using System;
using UnityEngine;
using UpscaleSDK.Core;
using UpscaleSDK.Core.Configuration;
using UpscaleSDK.Core.Saves.FileSystems;
#if SUPPORT_XBOX
using Unity.XGamingRuntime;
using UnityEngine.GameCore;
using UpscaleSDK.Core.Utils;
#endif

namespace UpscaleSDK.Platform.Xbox
{
    /// <summary>
    /// Represents a xbox platform class.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class XboxPlatform : Core.Platform
    {
        /// <summary>
        /// Occurs when initialized.
        /// </summary>
        public static event Action OnInitialized;
        /// <summary>
        /// Occurs when user added.
        /// </summary>
        public static event Action OnUserAdded;

        /// <summary>
        /// Gets or sets the is user ready.
        /// </summary>
        public static bool IsUserReady { get; private set; }
        /// <summary>
        /// Gets or sets the is achievements ready.
        /// </summary>
        public static bool IsAchievementsReady { get; private set; }
        /// <summary>
        /// Gets or sets the is saves ready.
        /// </summary>
        public static bool IsSavesReady { get; private set; }
        
        private XboxData _data;
        private XboxAchievements achievements;

        private static XboxPlatform _instance;
        
#if !UNITY_EDITOR || SUPPORT_XBOX
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        private static void RegisterPlatform()
        {
            PlatformProvider.SetPlatform<XboxPlatform>();
        }
#endif
        /// <summary>
        /// Provide file system.
        /// </summary>
        public override IFileSystem ProvideFileSystem()
        {
            return new XboxFileSystem(ConfigurationProvider.GetConfiguration().GetSavesSettings());
        }
        
#if SUPPORT_XBOX
        /// <summary>
        /// Gets the data.
        /// </summary>
        public static XboxData GetData()
        {
            return _instance._data;
        }
#endif
        
#if SUPPORT_XBOX
        private void Update()
        {
            UpdateService();
        }

        /// <summary>
        /// Update service.
        /// </summary>
        public void UpdateService()
        {
            if (Application.isEditor) 
                 return; 
            SDK.XTaskQueueDispatch(32);
        }
#endif


        /// <summary>
        /// Initialize.
        /// </summary>
        public override void Initialize()
        {
#if SUPPORT_XBOX
            UPSLogger.SavesInfoLog("Xbox start initialize");
            _instance = this;
            _data = new XboxData();
            achievements = new XboxAchievements(_data);

            int hr = SDK.CreateDefaultTaskQueue();
            if (HR.FAILED(hr))
            {
                UPSLogger.SavesErrorLog("Xbox error in CreateDefaultTaskQueue");
                return;
            }

            hr = SDK.XGameRuntimeInitialize();
            if (HR.FAILED(hr))
            {
                UPSLogger.SavesErrorLog("Xbox error in XGameRuntimeInitialize");
                return;
            }

            bool retrieveSystemInfoResult = RetrieveSystemInfo(_data);
            if (retrieveSystemInfoResult == false)
            {
                UPSLogger.SavesErrorLog("Xbox error in RetrieveSystemInfo");
                return;
            }

            hr = SDK.XBL.XblInitialize(GameCoreSettings.SCID);

            if (HR.FAILED(hr))
            {
                UPSLogger.SavesErrorLog("Xbox error in XblInitialize");
                return;
            }

            bool signInResult = SignIn(_data);
            UPSLogger.SavesInfoLog($"Xbox sign result is {signInResult}");
            if (signInResult == false)
            {
                return;
            }
            UPSLogger.SavesInfoLog("Xbox complete initialize");

            OnInitialized?.Invoke();
#endif
        }
        
#if SUPPORT_XBOX
        private bool RetrieveSystemInfo(XboxData xboxData)
        {
            int hr = SDK.XSystemGetXboxLiveSandboxId(out string xboxDataSandboxId);
            if (HR.FAILED(hr))
            {
                return false;
            }

            xboxData.SandboxId = xboxDataSandboxId;
            hr = SDK.XGameGetXboxTitleId(out uint titleId);
            if (HR.FAILED(hr))
            {
                return false;
            }

            xboxData.TitleId = titleId;
            xboxData.ServiceConfigId = $"00000000-0000-0000-0000-0000{titleId:X8}";
            return true;
        }

        private bool SignIn(XboxData xboxData, bool silently = true)
        {
            SDK.XUserAddAsync(silently ? XUserAddOptions.AddDefaultUserSilently : XUserAddOptions.None,
                (int hr, XUserHandle handle) =>
                {
                    if (HR.FAILED(hr))
                    {
                        SignIn(xboxData, false);
                        return;
                    }

                    ClearUser(xboxData);
                    xboxData.UserHandle = handle;
                    RetrieveUser(xboxData);
                    InitializeSaves(xboxData);                 
                    GetAchievements(xboxData);
                });
            return true;
        }

        private void RetrieveUser(XboxData xboxData)
        {
            
            int hr = SDK.XBL.XblContextCreateHandle(xboxData.UserHandle, out XblContextHandle contextHandle);
            xboxData.ContextHandle = contextHandle;
            hr = SDK.XUserGetId(xboxData.UserHandle, out ulong userId);
            xboxData.UserId = userId;
            hr = SDK.XUserGetGamertag(xboxData.UserHandle, XUserGamertagComponent.Classic, out string gamerTag);
            xboxData.GamerTag = gamerTag;
            IsUserReady = true;
            OnUserAdded?.Invoke();
        }

        private void InitializeSaves(XboxData data)
        {
            int hresult = SDK.XGameSaveInitializeProvider(data.UserHandle, data.ServiceConfigId, false, out var providerHandle);

            if (HR.FAILED(hresult))
            {
                UPSLogger.SavesErrorLog("Xbox error in XblAchievementsGetAchievementsForTitleIdAsync");
                return;
            }

            data.ProviderHandle = providerHandle;
            IsSavesReady = true;
            InvokeOnSavesInitialized();

        }

        private void ClearUser(XboxData xboxData)
        {
            if (xboxData.UserHandle != null)
            {
                SDK.XUserCloseHandle(xboxData.UserHandle);
                xboxData.UserHandle = null;
            }

            if (xboxData.ContextHandle != null)
            {
                SDK.XBL.XblContextCloseHandle(xboxData.ContextHandle);
                xboxData.ContextHandle = null;
            }

            xboxData.GamerTag = "";
            xboxData.UserId = 0;
        }

        private void GetAchievements(XboxData xboxData)
        {
            const int skippedAchievements = 0;
            const int numberOfAchievements = int.MaxValue;

            SDK.XBL.XblAchievementsGetAchievementsForTitleIdAsync(xboxData.ContextHandle, xboxData.UserId, xboxData.TitleId, XblAchievementType.All, false, XblAchievementOrderBy.DefaultOrder, skippedAchievements, numberOfAchievements, (int hr, XblAchievementsResultHandle result) =>
            {
                if (HR.FAILED(hr))
                {
                    UPSLogger.SavesErrorLog("Xbox error in XblAchievementsGetAchievementsForTitleIdAsync");
                    return;
                }

                hr = SDK.XBL.XblAchievementsResultGetAchievements(result, out XblAchievement[] achievements);

                if (HR.FAILED(hr))
                {
                    UPSLogger.SavesErrorLog("Xbox error in XblAchievementsResultGetAchievements");
                    return;
                }

                foreach (XblAchievement achievement in achievements)
                {
                    if (xboxData.Achievements.ContainsKey(achievement.Id))
                    {
                        xboxData.Achievements[achievement.Id] = achievement;
                        continue;
                    }
                    UPSLogger.SavesInfoLog($"Xbox Find ach {achievement.Name}");

                    xboxData.Achievements.Add(achievement.Id, achievement);
                }
                IsAchievementsReady = true;
            });
        }

        private void OnDestroy()
        {
            InvokeOnContextRequestSave();
            SDK.XGameSaveCloseProvider(_data.ProviderHandle);
            SDK.XUserCloseHandle(_data.UserHandle);
            SDK.XGameRuntimeUninitialize();
        }
        
        private void OnApplicationFocus(bool focus)
        {
            if (focus == false)
            {
               InvokeOnContextRequestSave();
            }
        }

        private void OnApplicationQuit()
        {
            InvokeOnContextRequestSave();
        }
#endif
    }
}
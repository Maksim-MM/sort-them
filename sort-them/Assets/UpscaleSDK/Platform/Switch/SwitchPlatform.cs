using System;
using Plugins.UpscaleSDK.Saves.Runtime.Switch;
using UnityEngine;
using UpscaleSDK.Core;
using UpscaleSDK.Core.Configuration;
using UpscaleSDK.Core.Saves.FileSystems;
using UpscaleSDK.Core.Utils;

namespace Plugins.UpscaleSDK.Switch.Runtime.Initialization
{
    /// <summary>
    /// Represents a switch platform class.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class SwitchPlatform : Platform
    {
#if UNITY_SWITCH || UNITY_EDITOR
        /// <summary>
        /// Gets or sets the user handle.
        /// </summary>
        public static nn.account.UserHandle UserHandle => _userHandle;
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        public static nn.account.Uid UserId => _userId;
        private static nn.account.UserHandle _userHandle = new();
        private static nn.account.Uid _userId = new();
#endif

        private bool _initialized = false;

#if !UNITY_EDITOR && UNITY_SWITCH
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        private static void RegisterPlatform()
        {
            PlatformProvider.SetPlatform<SwitchPlatform>();
        }
#endif
        
        /// <summary>
        /// Initialize.
        /// </summary>
        public override void Initialize()
        {
            if (Application.isEditor) return;
            if (_initialized) return;
#if UNITY_SWITCH || UNITY_EDITOR
            UPSLogger.SavesInfoLog("Switch start initialize");
            nn.account.Account.Initialize();
            if (nn.account.Account.TryOpenPreselectedUser(ref _userHandle) == false)
            {
                nn.Nn.Abort("Failed to open preselected user.");
            }

            nn.Result result = nn.account.Account.GetUserId(ref _userId, _userHandle);
            result.abortUnlessSuccess();
            if (nn.fs.FileSystem.ResultTargetLocked.Includes(result))
            {
                nn.account.Nickname nickname = new nn.account.Nickname();
                nn.account.Account.GetNickname(ref nickname, _userId);
                UPSLogger.SavesErrorLog($"The save data for {nickname.name} is already mounted: {result.ToString()}");
            }
            UPSLogger.SavesInfoLog("Switch complete initialize");
#if UNITY_SWITCH
            UnityEngine.Switch.Notification.notificationMessageReceived += OnNotificationMessageReceived;
            UnityEngine.Switch.Notification.EnterExitRequestHandlingSection();
#endif
#endif
            _initialized = true;
            InvokeOnSavesInitialized();
        }

        /// <summary>
        /// Provide file system.
        /// </summary>
        public override IFileSystem ProvideFileSystem()
        {
            return new SwitchFileSystem(ConfigurationProvider.GetConfiguration().GetSavesSettings());
        }

#if UNITY_SWITCH
        private void OnNotificationMessageReceived(UnityEngine.Switch.Notification.Message message)
        {
            if (message == UnityEngine.Switch.Notification.Message.ExitRequest)
            {
                InvokeOnContextRequestSave();
                UnityEngine.Switch.Notification.LeaveExitRequestHandlingSection();
            }
        }
#endif
    }
}
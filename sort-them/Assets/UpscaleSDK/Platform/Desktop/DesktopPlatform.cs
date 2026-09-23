using UnityEngine;
using UpscaleSDK.Core;
using UpscaleSDK.Core.Saves.FileSystems;

namespace UpscaleSDK.Platform.Desktop
{
    /// <summary>
    /// Represents a desktop platform class.
    /// </summary>
    public class DesktopPlatform : Core.Platform
    {
        /// <summary>
        /// Initialize.
        /// </summary>
        public override void Initialize()
        {
            InvokeOnSavesInitialized();
        }

        /// <summary>
        /// Provide file system.
        /// </summary>
        public override IFileSystem ProvideFileSystem()
        {
            return new DesktopFileSystem();
        }

        private void OnDestroy()
        {
            InvokeOnContextRequestSave();
        }
    
#if UNITY_EDITOR || UNITY_STANDALONE
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        private static void RegisterPlatform()
        {
            PlatformProvider.SetPlatform<DesktopPlatform>();
        }
#endif
    }
}

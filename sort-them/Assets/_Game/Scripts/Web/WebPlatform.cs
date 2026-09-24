using UnityEngine;
using UpscaleSDK.Core;
using UpscaleSDK.Core.Saves.FileSystems;

namespace SortThem.Web
{
    public class WebPlatform : UpscaleSDK.Core.Platform
    {
        public override void Initialize()
        {
            InvokeOnSavesInitialized();
        }

        public override IFileSystem ProvideFileSystem()
        {
            return new WebFileSystem();
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) InvokeOnContextRequestSave();
        }

        void OnDestroy()
        {
            InvokeOnContextRequestSave();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        static void RegisterPlatform()
        {
            PlatformProvider.SetPlatform<WebPlatform>();
        }
    }
}

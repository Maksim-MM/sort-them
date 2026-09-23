using UnityEngine;
using UpscaleSDK.Core.Configuration;
using UpscaleSDK.Core.Input.Core;
using UpscaleSDK.Core.Saves;
using UpscaleSDK.Core.Utils;

namespace UpscaleSDK.Core
{
    /// <summary>
    /// Represents a runtime bootstrap class.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class RuntimeBootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            UPSLogger.CoreDevLog($"Creating UPSRuntime...");
            var go = new GameObject("[UPSRuntime]");
            go.AddComponent<RuntimeBootstrap>();
            DontDestroyOnLoad(go);
        }

        private void Awake()
        {
            var provider = gameObject.AddComponent<ConfigurationProvider>();
            
            bool result = provider.Initialize();
            if (!result)
            {
                return;
            }
            
            var platform = (Platform)gameObject.AddComponent(PlatformProvider.PlatformType);
            
            var input = gameObject.AddComponent<UPSInput>();
            input.Initialize();


            var saves = gameObject.AddComponent<UPSSaves>();
            saves.Initialize(platform);
            
            platform.Initialize();
        }
    }
}

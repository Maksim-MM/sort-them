using UnityEngine;
using UpscaleSDK.Core.Utils;

namespace UpscaleSDK.Core.Configuration
{
    /// <summary>
    /// Represents a configuration provider class.
    /// </summary>
    public class ConfigurationProvider : MonoBehaviour
    {
        private static ConfigurationProvider _instance;
        private UpscaleSDKConfig _cachedConfig;

        /// <summary>
        /// Initialize.
        /// </summary>
        public bool Initialize()
        {
            _instance = this;
            _cachedConfig = LoadConfiguration();
            
            if (_cachedConfig == null)
            {
                UPSLogger.CoreErrorLog("Can't load upscale configuration");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        public static UpscaleSDKConfig GetConfiguration()
        {
#if UNITY_EDITOR
            if (_instance == null)
            {
                return LoadConfiguration();
            }
#endif
            
            return _instance._cachedConfig;
        }

        private void OnDestroy()
        {
            _instance = null;
        }

        private static UpscaleSDKConfig LoadConfiguration()
        {
            return Resources.Load<UpscaleSDKConfig>("UpscaleSDK/Config");
        }
    }
}
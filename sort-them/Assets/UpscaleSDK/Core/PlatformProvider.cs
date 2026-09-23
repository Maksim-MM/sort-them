using System;
using UnityEngine;
using UpscaleSDK.Core.Utils;

namespace UpscaleSDK.Core
{
    /// <summary>
    /// Represents a platform provider class.
    /// </summary>
    public class PlatformProvider : MonoBehaviour
    {
        /// <summary>
        /// Gets or sets the platform type.
        /// </summary>
        public static Type PlatformType { get; private set; }

        /// <summary>
        /// Sets the platform.
        /// </summary>
        public static void SetPlatform<T>() where T : Platform
        {
            UPSLogger.CoreInfoLog($"Detect runtime platform [{typeof(T).Name}]!");
            PlatformType = typeof(T);
        }
    }
}

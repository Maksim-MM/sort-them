#if UNITY_EDITOR

using UnityEditor;
using UpscaleSDK.Core.Validator;

namespace UpscaleSDK.Platform.Desktop
{
    /// <summary>
    /// Represents a desktop validator class.
    /// </summary>
    public class DesktopValidator : Validator
    {
        /// <summary>
        /// Gets or sets the targets.
        /// </summary>
        public override BuildTarget[] Targets => new[]
        {
            BuildTarget.StandaloneWindows, 
            BuildTarget.StandaloneWindows64,
            BuildTarget.StandaloneLinux64,
            BuildTarget.StandaloneOSX,
        };

        /// <summary>
        /// Validate.
        /// </summary>
        public override ValidationResult Validate()
        {
            ValidationResult result = new();
            ValidateCompanyName(result);
            ValidateAppVersion(result);
            ValidateIsPackageInstalled(result, "com.unity.inputsystem");
            ValidateIsNewInputEnabled(result);
            ValidateIfSplashScreenEnabled(result);
            ValidateIfGcIncremental(result);
            ValidateScenes(result);
            ValidateSDKConfiguration(result);
            return result;
        }
    }
}
#endif
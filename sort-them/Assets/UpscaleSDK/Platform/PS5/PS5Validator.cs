#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UpscaleSDK.Core.Validator;

namespace UpscaleSDK.Platform.PS5
{
    /// <summary>
    /// Represents a ps5 validator class.
    /// </summary>
    public class PS5Validator : Validator
    {
        /// <summary>
        /// Gets or sets the targets.
        /// </summary>
        public override BuildTarget[] Targets => new[] { BuildTarget.PS5 };

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
            
            ValidateIsPackageInstalled(result, "com.unity.inputsystem.ps5");
            ValidateIsPackageInstalled(result, "com.unity.psn.ps5");
            ValidateIsPackageInstalled(result, "com.unity.savedata.ps5");
            ValidateIsPackageInstalled(result, "com.unity.render-pipelines.ps5");
            ValidateIsPackageInstalled(result, "com.unity.commondialog.ps5");
            ValidateIsPackageInstalled(result, "com.unity.share.ps5");
            ValidateIsPackageInstalled(result, "com.unity.playgo.ps5");
#if UNITY_PS5
            Validate(result, string.IsNullOrEmpty(UnityEditor.PS5.PlayerSettings.paramFilePath) == false, "Param File is not set");
            Validate(result, string.IsNullOrEmpty(UnityEditor.PS5.PlayerSettings.npConfigZipPath) == false, "Package Metadata File (npconfig.zip) is not set");
            Validate(result,string.IsNullOrEmpty(UnityEditor.PS5.PlayerSettings.backgroundImagePath) == false, "Background Image is not set");
            Validate(result,string.IsNullOrEmpty(UnityEditor.PS5.PlayerSettings.startupBackgroundImagePath) == false, "Start-up Image (Background) is not set");
            Validate(result,string.IsNullOrEmpty(UnityEditor.PS5.PlayerSettings.startupForegroundImagePath) == false, "Start-up Image (Foreground) is not set");
#endif 
            return result;
        }
        
        
    }
}
#endif
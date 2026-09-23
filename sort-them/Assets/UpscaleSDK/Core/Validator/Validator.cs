#if UNITY_EDITOR
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UpscaleSDK.Core.Configuration;

namespace UpscaleSDK.Core.Validator
{
    /// <summary>
    /// Represents a validator message type enum.
    /// </summary>
    public enum ValidatorMessageType
    {
        /// <summary>
        /// Error.
        /// </summary>
        Error,
        /// <summary>
        /// Warning.
        /// </summary>
        Warning,
    }
    
    /// <summary>
    /// Represents a validator class.
    /// </summary>
    public abstract class Validator
    {
        /// <summary>
        /// Gets or sets the targets.
        /// </summary>
        public abstract BuildTarget[] Targets { get; }
        /// <summary>
        /// Validate.
        /// </summary>
        public abstract ValidationResult Validate();

        /// <summary>
        /// Validate company name.
        /// </summary>
        /// <param name="result">The result.</param>
        protected void ValidateCompanyName(ValidationResult result)
        {
            if (UnityEngine.Application.companyName != "Upscale Studio")
            {
                result.AddError("Upscale Studio is not listed as a company name");
            }
        }

        /// <summary>
        /// Validate app version.
        /// </summary>
        /// <param name="result">The result.</param>
        protected void ValidateAppVersion(ValidationResult result)
        {
            if (Regex.IsMatch(UnityEngine.Application.version, @"^\d+\.\d+\.\d+$") == false)
            {
                result.AddError("Incorrect game version (use the X.Y.Z format)");
            }
        }
    
        /// <summary>
        /// Validate is new input enabled.
        /// </summary>
        /// <param name="result">The result.</param>
        protected void ValidateIsNewInputEnabled(ValidationResult result)
        {
            #if !ENABLE_INPUT_SYSTEM
                   result.AddError("Unity Input System is not enabled");
            #endif
        }
    
        /// <summary>
        /// Validate is package installed.
        /// </summary>
        /// <param name="result">The result.</param>
        /// <param name="packageName">The package name.</param>
        protected void ValidateIsPackageInstalled(ValidationResult result, string packageName)
        {
            bool isInstalled = System.IO.File.ReadAllText("Packages/packages-lock.json").Contains(packageName);
            if (isInstalled == false)
            {
                result.AddError($"Package '{packageName}' is not installed.");
            }
        }

        /// <summary>
        /// Validate if splash screen enabled.
        /// </summary>
        /// <param name="result">The result.</param>
        protected void ValidateIfSplashScreenEnabled(ValidationResult result)
        {
            if (PlayerSettings.SplashScreen.show)
            {
                result.AddWarning("Splash screen is enabled. Disable it if possible");
            }
        }
    
        /// <summary>
        /// Validate if gc incremental.
        /// </summary>
        /// <param name="result">The result.</param>
        protected void ValidateIfGcIncremental(ValidationResult result)
        {
            if (PlayerSettings.gcIncremental == false)
            {
                result.AddWarning("Incremental garbage collector is not enabled");
            }
        }
    
        /// <summary>
        /// Validate scenes.
        /// </summary>
        /// <param name="result">The result.</param>
        protected void ValidateScenes(ValidationResult result)
        {
            EditorBuildSettingsScene[] scenes =
                EditorBuildSettings.scenes;
        
            if (scenes.Length == 0)
            {
                result.AddError("No scenes added to Build Settings");
                return;
            }

            bool hasBootstrapScene =
                scenes.Any(scene =>
                    scene.path.Contains("UPSBootstrap"));

            if (!hasBootstrapScene)
            {
                result.AddWarning(
                    "Bootstrap scene is missing from Build Settings");
            }
        }

        /// <summary>
        /// Validate sdk configuration.
        /// </summary>
        /// <param name="result">The result.</param>
        protected void ValidateSDKConfiguration(ValidationResult result)
        {
            if (ConfigurationProvider.GetConfiguration() == null)
            {
                result.AddError("Can't find Upscale SDK configuration");
            }
        }
        
        /// <summary>
        /// Validate.
        /// </summary>
        /// <param name="result">The result.</param>
        /// <param name="requiredCondition">The required condition.</param>
        /// <param name="message">The message.</param>
        /// <param name="messageType">The message type.</param>
        public static void Validate(ValidationResult result, bool requiredCondition, string message, ValidatorMessageType messageType = ValidatorMessageType.Error)
        {
            if (requiredCondition == false)
            {
                if (messageType == ValidatorMessageType.Error)
                {
                    result.AddError(message);
                }
                else
                {
                    result.AddWarning(message);
                }
            }
        }
    }
}
#endif
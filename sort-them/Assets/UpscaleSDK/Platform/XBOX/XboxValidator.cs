#if UNITY_EDITOR
using UnityEditor;
using UpscaleSDK.Core.Validator;

namespace UpscaleSDK.Platform.Xbox
{
    /// <summary>
    /// Represents a xbox validator class.
    /// </summary>
    public class XboxValidator : Validator
    {
        /// <summary>
        /// Gets or sets the targets.
        /// </summary>
        public override BuildTarget[] Targets => new[] { BuildTarget.GameCoreXboxOne, BuildTarget.GameCoreXboxSeries , BuildTarget.XboxOne};

        /// <summary>
        /// Validate.
        /// </summary>
        public override ValidationResult Validate()
        {
            ValidationResult result = new();
            ValidateIsPackageInstalled(result, "com.unity.microsoft.gdk");
            ValidateIsPackageInstalled(result, "com.unity.microsoft.gdk.tools");
            ValidateIsPackageInstalled(result, "com.unity.microsoft.gdk.tools.xbox");
            ValidateIsPackageInstalled(result, "com.unity.inputsystem.gxdk");
            ValidateIsPackageInstalled(result, "com.unity.render-pipelines.gamecore");

            return result;
        }
    }
}
#endif
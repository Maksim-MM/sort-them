#if UNITY_EDITOR
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace UpscaleSDK.Core.Validator
{
    /// <summary>
    /// Represents a upscale sdk build validator class.
    /// </summary>
    public class UpscaleSDKBuildValidator : IPreprocessBuildWithReport
    {
        /// <summary>
        /// Gets or sets the callback order.
        /// </summary>
        public int callbackOrder => -1000;

        /// <summary>
        /// Handles the preprocess build.
        /// </summary>
        /// <param name="report">The report.</param>
        public void OnPreprocessBuild(BuildReport report)
        {
            ValidationResult result =
                UpscaleSDKValidatorRunner.Validate(report.summary.platform);

            if (!result.IsValid)
            {
                Debug.LogError(
                    "UpscaleSDK validation failed. Check console for details.");
            }
        }
    }
}
#endif
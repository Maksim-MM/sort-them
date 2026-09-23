#if UNITY_EDITOR

using UnityEditor;

namespace UpscaleSDK.Core.Validator
{
    /// <summary>
    /// Represents a upscale sdk validation menu class.
    /// </summary>
    public static class UpscaleSDKValidationMenu
    {
        /// <summary>
        /// Validate.
        /// </summary>
        [MenuItem("Tools/UpscaleSDK/Validate")]
        public static void Validate()
        {
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;

            UpscaleSDKValidatorRunner.Validate(target);
        }
    }
}
#endif
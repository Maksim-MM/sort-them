#if UNITY_GAMECORE_XBOXSERIES || UNITY_GAMECORE_XBOXONE
#define SUPPORT_XBOX
using System.Collections.Generic;
using Unity.XGamingRuntime;
#endif
namespace UpscaleSDK.Platform.Xbox
{
    /// <summary>
    /// Represents a xbox data class.
    /// </summary>
    public class XboxData
    {
#if SUPPORT_XBOX
        /// <summary>
        /// Gets or sets the service config id.
        /// </summary>
        public string ServiceConfigId { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the sandbox id.
        /// </summary>
        public string SandboxId { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the title id.
        /// </summary>
        public uint TitleId { get; set; } = uint.MinValue;
        /// <summary>
        /// Gets or sets the gamer tag.
        /// </summary>
        public string GamerTag { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        public ulong UserId { get; set; } = ulong.MinValue;
        /// <summary>
        /// Gets or sets the context handle.
        /// </summary>
        public XblContextHandle ContextHandle { get; set; } = null;
        /// <summary>
        /// Gets or sets the user handle.
        /// </summary>
        public XUserHandle UserHandle { get; set; } = null;
        /// <summary>
        /// Gets or sets the achievements.
        /// </summary>
        public Dictionary<string, XblAchievement> Achievements { get; set; } = new();
        /// <summary>
        /// Gets or sets the provider handle.
        /// </summary>
        public XGameSaveProviderHandle ProviderHandle { get; set; } = null;
#endif
    }
}
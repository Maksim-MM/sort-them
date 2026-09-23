namespace Plugins.UpscaleSDK.PS4.Runtime.App
{
    /// <summary>
    /// Represents a app data class.
    /// </summary>
    public class AppData
    {
        /// <summary>
        /// Gets or sets the np title id.
        /// </summary>
        public string NpTitleId { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the is demo application.
        /// </summary>
        public bool IsDemoApplication { get; set; } = false;
        /// <summary>
        /// Gets or sets the can use social.
        /// </summary>
        public bool CanUseSocial { get; set; } = false;
    }
}
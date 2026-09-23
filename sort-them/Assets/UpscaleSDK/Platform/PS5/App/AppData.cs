namespace Plugins.UpscaleSDK.PS5.Runtime.App
{
    /// <summary>
    /// Represents a app data class.
    /// </summary>
    public class AppData
    {
        /// <summary>
        /// Gets or sets the content id.
        /// </summary>
        public string ContentId { get; set; }
        /// <summary>
        /// Gets or sets the is demo.
        /// </summary>
        public bool IsDemo { get; set; }
        /// <summary>
        /// Gets or sets the is editor.
        /// </summary>
        public bool IsEditor { get; set; }
        /// <summary>
        /// Gets or sets the can use social.
        /// </summary>
        public bool CanUseSocial { get; set; }
    }
}
#if UNITY_PS5
using UnityEngine.PS5;
#endif

namespace Plugins.UpscaleSDK.PS5.Runtime.Users
{
    /// <summary>
    /// Represents a user data class.
    /// </summary>
    public class UserData
    {
#if UNITY_PS5
        /// <summary>
        /// Gets or sets the ps user.
        /// </summary>
        public PS5Input.LoggedInUser PSUser { get; set; }
#endif
    }
}
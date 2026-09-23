#if UNITY_PS4
using UnityEngine.PS4;
#endif

namespace Plugins.UpscaleSDK.PS4.Runtime.User
{
    /// <summary>
    /// Represents a user data class.
    /// </summary>
    public class UserData
    {
#if UNITY_PS4
        /// <summary>
        /// Gets or sets the ps4 user.
        /// </summary>
        public PS4Input.LoggedInUser PS4User { get; set; }
#endif
    }
}
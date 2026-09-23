#if UNITY_PS5
using Unity.PSN.PS5.Trophies;
#endif

namespace Plugins.UpscaleSDK.PS5.Runtime.Users.Trophies
{
    /// <summary>
    /// Represents a trophy class.
    /// </summary>
    public class Trophy
    {
#if UNITY_PS5
        /// <summary>
        /// The details field.
        /// </summary>
        public TrophySystem.TrophyDetails Details;
        /// <summary>
        /// The data field.
        /// </summary>
        public TrophySystem.TrophyData Data;
#endif
    }
}
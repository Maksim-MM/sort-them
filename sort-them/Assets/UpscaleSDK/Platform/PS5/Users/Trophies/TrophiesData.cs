#if UNITY_PS5
using System.Collections.Generic;
using Unity.PSN.PS5.Trophies;
#endif

namespace Plugins.UpscaleSDK.PS5.Runtime.Users.Trophies
{
    /// <summary>
    /// Represents a trophies data class.
    /// </summary>
    public class TrophiesData
    {
#if UNITY_PS5
        /// <summary>
        /// The game details field.
        /// </summary>
        public TrophySystem.TrophyGameDetails GameDetails;
        /// <summary>
        /// The game data field.
        /// </summary>
        public TrophySystem.TrophyGameData GameData;
        /// <summary>
        /// The group details field.
        /// </summary>
        public TrophySystem.TrophyGroupDetails GroupDetails;
        /// <summary>
        /// The group data field.
        /// </summary>
        public TrophySystem.TrophyGroupData GroupData;
        /// <summary>
        /// The all trophies field.
        /// </summary>
        public Dictionary<int, Trophy> AllTrophies;
#endif
    }
}
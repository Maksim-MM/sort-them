using Plugins.UpscaleSDK.PS4.Runtime.App;
using Plugins.UpscaleSDK.PS4.Runtime.User;
using UnityEngine;
#if UNITY_PS4
using Sony.NP;
#endif

namespace Plugins.UpscaleSDK.PS4.Runtime.TrophiesManagement
{
    /// <summary>
    /// Represents a ps4 trophies class.
    /// </summary>
    public class Ps4Trophies
    {
        private readonly AppData _appData;
        private readonly UserData _userData;

        private static Ps4Trophies _instance;

        public Ps4Trophies(AppData appData, UserData userData)
        {
            _appData = appData;
            _userData = userData;
            _instance = this;
        }

        /// <summary>
        /// Unlock.
        /// </summary>
        /// <param name="id">The id.</param>
        public static void Unlock(int id)
        {
            if (_instance == null)
            {
                return;
            }
            _instance.UnlockTrophy(id);
        }

        /// <summary>
        /// Unlocks a trophy for the current user.
        /// </summary>
        /// <param name="trophyId">ID of a trophy that will be unlocked</param>
        public void UnlockTrophy(int trophyId)
        {
            #if UNITY_PS4
            if (_appData.CanUseSocial == false) return;

            try
            {
                Trophies.UnlockTrophyRequest request = new()
                {
                    UserId = _userData.PS4User.userId,
                    TrophyId = trophyId
                };

                Core.EmptyResponse response = new();
                int requestId = Trophies.UnlockTrophy(request, response);
            }
            catch (NpToolkitException ex)
            {
                Debug.LogError($"Error while trying to unlock trophy: {ex}");
            }
            #endif
        }
    }
}
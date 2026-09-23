using System;
using Plugins.UpscaleSDK.PS5.Runtime.App;
using Plugins.UpscaleSDK.PS5.Runtime.Common;
#if UNITY_PS5
using Unity.PSN.PS5.Aysnc;
using Unity.PSN.PS5.UDS;
#endif

namespace Plugins.UpscaleSDK.PS5.Runtime.Users.Trophies
{
    /// <summary>
    /// Represents a ps5 trophies class.
    /// </summary>
    public class Ps5Trophies
    {
        /// <summary>
        /// Occurs when trophy unlocked.
        /// </summary>
        public event Action<int> OnTrophyUnlocked;
        
        private readonly AppData _appData;
        private readonly UserData _userData;
        private readonly TrophiesData _trophiesData;
        private static Ps5Trophies _instance;

        public Ps5Trophies(AppData appData, UserData userData, TrophiesData trophiesData)
        {
            _appData = appData;
            _userData = userData;
            _trophiesData = trophiesData;
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
            _instance.Unlock(id);
        }

        /// <summary>
        /// Unlocks the trophy with the given ID.
        /// </summary>
        /// <param name="trophyId">ID of a trophy</param>
        public void Unlock(int trophyId, Action callback = null)
        {
#if UNITY_PS5
            if (_appData.CanUseSocial == false) return;
            if (_trophiesData.AllTrophies.ContainsKey(trophyId) && _trophiesData.AllTrophies[trophyId].Data.Unlocked)
                return;
            
            UniversalDataSystem.UnlockTrophyRequest request = new()
            {
                UserId = _userData.PSUser.userId,
                TrophyId = trophyId
            };

            var requestOperation = new AsyncRequest<UniversalDataSystem.UnlockTrophyRequest>(request).ContinueWith((
                antecedent) =>
            {
                if (antecedent.IsSuccess())
                {
                    callback?.Invoke();
                    OnTrophyUnlocked?.Invoke(trophyId);
                }
            });
            
            UniversalDataSystem.Schedule(requestOperation);
#endif
        }

        /// <summary>
        /// Updates the progress of a trophy with the given ID.
        /// </summary>
        /// <param name="trophyId">ID of a trophy</param>
        /// <param name="progress">New progress</param>
        public void UnlockProgress(int trophyId, long progress, Action callback = null)
        {
#if UNITY_PS5
            if (_appData.CanUseSocial == false) return;
            UniversalDataSystem.UpdateTrophyProgressRequest request = new()
            {
                UserId = _userData.PSUser.userId,
                TrophyId = trophyId,
                Progress = progress
            };
            
            var requestOperation = new AsyncRequest<UniversalDataSystem.UpdateTrophyProgressRequest>(request).ContinueWith((
                antecedent) =>
            {
                if (antecedent.IsSuccess())
                {
                    callback?.Invoke();
                    OnTrophyUnlocked?.Invoke(trophyId);
                }
            });
            
            UniversalDataSystem.Schedule(requestOperation);
#endif
}
        
        /// <summary>
        /// Checks if the trophy with the given ID is unlocked.
        /// </summary>
        /// <param name="trophyId">ID of trophy</param>
        /// <returns>Trophy status</returns>
        public bool IsTrophyUnlocked(int trophyId)
        {
#if UNITY_PS5
            if (_appData.CanUseSocial == false) return false;
            if (_trophiesData.AllTrophies.ContainsKey(trophyId))
                return _trophiesData.AllTrophies[trophyId].Data.Unlocked;
            return false;
#endif
            return false;
        }
    }
}
#if UNITY_GAMECORE_XBOXSERIES || UNITY_GAMECORE_XBOXONE
#define SUPPORT_XBOX
using Unity.XGamingRuntime;
#endif

namespace UpscaleSDK.Platform.Xbox
{
    /// <summary>
    /// Represents a xbox achievements class.
    /// </summary>
    public class XboxAchievements
    {
        private readonly XboxData _xboxData;

        private static XboxAchievements _instance;

        public XboxAchievements(XboxData xboxData)
        {
            _xboxData = xboxData;
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

            _instance.UnlockAchievement(id);
        }

        /// <summary>
        /// Unlock achievement.
        /// </summary>
        /// <param name="achievementId">The achievement id.</param>
        public void UnlockAchievement(int achievementId)
        {
#if SUPPORT_XBOX
            string id = achievementId.ToString();

            if (_xboxData.Achievements.ContainsKey(id) &&
                _xboxData.Achievements[id].ProgressState == XblAchievementProgressState.Achieved)
            {
                return;
            }

            SDK.XBL.XblAchievementsUpdateAchievementAsync(_xboxData.ContextHandle, _xboxData.UserId, id, 100,
                (int hr) =>
                {
                    if (HR.FAILED(hr))
                    {
                        return;
                    }

                    GetAchievement(achievementId);
                });
#endif
        }

        private void GetAchievement(int achievementId)
        {
#if SUPPORT_XBOX
            SDK.XBL.XblAchievementsGetAchievementAsync(_xboxData.ContextHandle, _xboxData.UserId, _xboxData.ServiceConfigId, achievementId.ToString(), (int hr, XblAchievementsResultHandle result) =>
            {
                if (HR.FAILED(hr))
                {
                    return;
                }

                hr = SDK.XBL.XblAchievementsResultGetAchievements(result, out XblAchievement[] achievements);

                if (HR.FAILED(hr))
                {
                    return;
                }

                foreach (XblAchievement achievement in achievements)
                {
                    if (_xboxData.Achievements.ContainsKey(achievement.Id))
                    {
                        _xboxData.Achievements[achievement.Id] = achievement;
                        continue;
                    }

                    _xboxData.Achievements.Add(achievement.Id, achievement);
                }
            });
#endif
        }
    }
}
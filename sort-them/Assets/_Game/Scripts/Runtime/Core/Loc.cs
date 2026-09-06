using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace SortThem
{
    public static class Loc
    {
        public const string Table = "Game";
        public static bool Ready;

        public static string Get(LocalizedString ls, string fallback)
        {
            if (!Ready || ls == null || ls.IsEmpty) return fallback;
            try
            {
                var s = ls.GetLocalizedString();
                return string.IsNullOrEmpty(s) ? fallback : s;
            }
            catch
            {
                return fallback;
            }
        }

        public static string Get(string key, string fallback)
        {
            if (!Ready) return fallback;
            try
            {
                var s = LocalizationSettings.StringDatabase.GetLocalizedString(Table, key);
                return string.IsNullOrEmpty(s) ? fallback : s;
            }
            catch
            {
                return fallback;
            }
        }
    }
}

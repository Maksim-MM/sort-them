using UnityEngine;

namespace SortThem
{
    public static class Platform
    {
        public const string EmulateMobilePref = "SortThem.EmulateMobile";
        static bool? _mobile;

        public static bool IsMobile
        {
            get
            {
                if (_mobile.HasValue) return _mobile.Value;
#if UNITY_EDITOR
                if (UnityEditor.EditorPrefs.GetBool(EmulateMobilePref, false)) return true;
#endif
                return Application.isMobilePlatform;
            }
            set => _mobile = value;
        }
    }
}

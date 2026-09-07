using UnityEngine;

namespace SortThem
{
    public static class Platform
    {
        static bool? _mobile;

        public static bool IsMobile
        {
            get => _mobile ?? Application.isMobilePlatform;
            set => _mobile = value;
        }
    }
}

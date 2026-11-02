using System;
using System.Collections;
using UnityEngine;

namespace SortThem
{
    public static class Platform
    {
        public const string EmulateMobilePref = "SortThem.EmulateMobile";
        public const string EmulateConsolePref = "SortThem.EmulateConsole";
        static bool? _mobile;
        static bool? _shadows;

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

        public static bool IsWeb
        {
            get
            {
#if UNITY_WEBGL
                return true;
#else
                return false;
#endif
            }
        }

        public static bool IsPlayStation
        {
            get
            {
#if UNITY_PS4 || UNITY_PS5
                return true;
#else
                return false;
#endif
            }
        }

        public static bool HasLightBar => IsPlayStation || (Application.isEditor && !IsMobile);

        public static bool IsConsoleHQ
        {
            get
            {
#if UNITY_PS4 || UNITY_PS5 || UNITY_GAMECORE_XBOXSERIES || UNITY_GAMECORE_XBOXONE
                return true;
#else
                return false;
#endif
            }
        }

        public static bool HighEnd
        {
            get
            {
                if (_shadows.HasValue) return _shadows.Value;
#if UNITY_EDITOR
                if (UnityEditor.EditorPrefs.GetBool(EmulateConsolePref, false)) return true;
#endif
                return IsConsoleHQ;
            }
            set => _shadows = value;
        }

        public static IEnumerator WaitUntil(Func<bool> done, float seconds, int minFrames = 300)
        {
            float deadline = Time.realtimeSinceStartup + seconds;
            int frames = 0;
            while (!done() && (Time.realtimeSinceStartup < deadline || frames < minFrames))
            {
                frames++;
                yield return null;
            }
        }

        public static event Action GameReady;
        public static void RaiseGameReady() => GameReady?.Invoke();

        public const int SwitchFrameRate = 30;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void ApplyFrameRate()
        {
#if UNITY_SWITCH && !UNITY_EDITOR
            QualitySettings.vSyncCount = 60 / SwitchFrameRate;
            Application.targetFrameRate = SwitchFrameRate;
#endif
        }

        public static bool LowPower
        {
            get
            {
#if UNITY_SWITCH
                return true;
#else
                return IsMobile;
#endif
            }
        }
    }
}

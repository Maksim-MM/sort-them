using System;
using UnityEngine;

namespace SortThem
{
    public static class Settings
    {
        public const float SensitivityMin = 0.2f, SensitivityMax = 3f;

        public static float MusicVolume { get; private set; } = 0.7f;
        public static float SfxVolume { get; private set; } = 1f;
        public static float SensitivityX { get; private set; } = 1f;
        public static float SensitivityY { get; private set; } = 1f;
        public static bool Vibration { get; private set; } = true;
        public static int MusicTrack { get; private set; }

        public static event Action Changed;

        static bool _loaded;

        public static void Load()
        {
            MusicVolume = PlayerPrefs.GetFloat("settings.music", 0.7f);
            SfxVolume = PlayerPrefs.GetFloat("settings.sfx", 1f);
            SensitivityX = PlayerPrefs.GetFloat("settings.sens_x", 1f);
            SensitivityY = PlayerPrefs.GetFloat("settings.sens_y", 1f);
            Vibration = PlayerPrefs.GetInt("settings.vibration", 1) != 0;
            MusicTrack = PlayerPrefs.GetInt("settings.track", 0);
            _loaded = true;
            Apply();
        }

        public static void EnsureLoaded() { if (!_loaded) Load(); }

        public static void SetMusicVolume(float v) { MusicVolume = Mathf.Clamp01(v); Store(); }
        public static void SetSfxVolume(float v) { SfxVolume = Mathf.Clamp01(v); Store(); }
        public static void SetSensitivityX(float v) { SensitivityX = Mathf.Clamp(v, SensitivityMin, SensitivityMax); Store(); }
        public static void SetSensitivityY(float v) { SensitivityY = Mathf.Clamp(v, SensitivityMin, SensitivityMax); Store(); }
        public static void SetVibration(bool on) { Vibration = on; Store(); }
        public static void SetMusicTrack(int index) { MusicTrack = Mathf.Max(0, index); Store(); }

        public static void Flush() => PlayerPrefs.Save();

        static void Store()
        {
            PlayerPrefs.SetFloat("settings.music", MusicVolume);
            PlayerPrefs.SetFloat("settings.sfx", SfxVolume);
            PlayerPrefs.SetFloat("settings.sens_x", SensitivityX);
            PlayerPrefs.SetFloat("settings.sens_y", SensitivityY);
            PlayerPrefs.SetInt("settings.vibration", Vibration ? 1 : 0);
            PlayerPrefs.SetInt("settings.track", MusicTrack);
            Apply();
        }

        static void Apply()
        {
            Sfx.Volume = SfxVolume;
            Changed?.Invoke();
        }
    }
}

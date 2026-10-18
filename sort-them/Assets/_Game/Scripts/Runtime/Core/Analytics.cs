using System;
using System.Collections.Generic;
using UnityEngine;

namespace SortThem
{
    public static class Analytics
    {
        public static event Action<string, Dictionary<string, object>> Sent;

        public static bool Enabled => Sent != null;

        public static void Track(string name, Dictionary<string, object> data = null) => Sent?.Invoke(name, data);

        public static int SecondsPlayed => Mathf.RoundToInt(Time.realtimeSinceStartup);

        public static bool FirstTime(string key)
        {
            if (!Enabled) return false;
            string k = "analytics." + key;
            if (PlayerPrefs.GetInt(k, 0) != 0) return false;
            PlayerPrefs.SetInt(k, 1);
            PlayerPrefs.Save();
            return true;
        }

        public static int NextAttempt(string key)
        {
            if (!Enabled) return 0;
            string k = "analytics." + key;
            int n = PlayerPrefs.GetInt(k, 0) + 1;
            PlayerPrefs.SetInt(k, n);
            PlayerPrefs.Save();
            return n;
        }

        public static Dictionary<string, object> Window(bool first)
        {
            var gm = GameManager.I;
            return new Dictionary<string, object>
            {
                { "seconds_played", SecondsPlayed },
                { "balance", gm != null && gm.Economy != null ? Mathf.RoundToInt(gm.Economy.Balance) : 0 },
                { "first", first }
            };
        }
    }
}

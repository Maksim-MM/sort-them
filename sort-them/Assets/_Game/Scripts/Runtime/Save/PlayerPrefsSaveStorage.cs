using System;
using UnityEngine;

namespace SortThem
{
    public class PlayerPrefsSaveStorage : ISaveStorage
    {
        const string Key = "sortthem.save";

        public bool TryLoad(out byte[] data)
        {
            data = null;
            var s = PlayerPrefs.GetString(Key, null);
            if (string.IsNullOrEmpty(s)) return false;
            try
            {
                data = Convert.FromBase64String(s);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void Save(byte[] data)
        {
            PlayerPrefs.SetString(Key, Convert.ToBase64String(data));
            PlayerPrefs.Save();
        }

        public void Clear()
        {
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
        }
    }
}

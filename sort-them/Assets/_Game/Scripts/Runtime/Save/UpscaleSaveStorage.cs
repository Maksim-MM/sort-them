using System;
using UnityEngine;

namespace SortThem
{
    public class UpscaleSaveStorage : ISaveStorage
    {
        const string Key = "sortthem.save";
        readonly PlayerPrefsSaveStorage _legacy = new PlayerPrefsSaveStorage();

        public bool TryLoad(out byte[] data)
        {
            data = null;
            var prefs = SavesReady.Prefs;
            if (prefs == null) return _legacy.TryLoad(out data);
            string s = prefs.GetString(Key, null);
            if (string.IsNullOrEmpty(s))
            {
                if (!_legacy.TryLoad(out data)) return false;
                prefs.SetString(Key, Convert.ToBase64String(data));
                prefs.TrySave();
                Debug.Log("SortThem: save imported from PlayerPrefs into SDK storage");
                return true;
            }
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
            var prefs = SavesReady.Prefs;
            if (prefs == null) { _legacy.Save(data); return; }
            prefs.SetString(Key, Convert.ToBase64String(data));
            prefs.TrySave();
        }

        public void Clear()
        {
            var prefs = SavesReady.Prefs;
            _legacy.Clear();
            if (prefs == null) return;
            prefs.DeleteKey(Key);
            prefs.TrySave();
        }
    }
}

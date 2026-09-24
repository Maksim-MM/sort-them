using UpscaleSDK.Core.Saves;

namespace SortThem
{
    public static class SavesReady
    {
        public static bool IsReady
        {
            get
            {
                try { return UPSSaves.Prefs != null && UPSSaves.Prefs.IsReady; }
                catch { return false; }
            }
        }

        public static UPSPlayerPrefs Prefs
        {
            get
            {
                try { return UPSSaves.Prefs != null && UPSSaves.Prefs.IsReady ? UPSSaves.Prefs : null; }
                catch { return null; }
            }
        }
    }
}

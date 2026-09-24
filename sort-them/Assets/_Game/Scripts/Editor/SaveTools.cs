using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SortThem.Editor
{
    public static class SaveTools
    {
        static readonly string[] PrefKeys = { "sortthem.save", "settings.music", "settings.sfx", "settings.sens_x", "settings.sens_y", "settings.vibration", "settings.track", "settings.locale" };

        static string SdkSavePath => Path.Combine(Application.persistentDataPath, "UpscaleSaves", "SortThem.txt");

        [MenuItem("SortThem/Dev/Delete Saves (with backup)")]
        public static void DeleteSaves()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Delete Saves", "Останови Play: во время игры сейв перезапишется при выходе.", "OK");
                return;
            }
            bool sdk = File.Exists(SdkSavePath);
            bool prefs = PlayerPrefs.HasKey("sortthem.save");
            if (!sdk && !prefs)
            {
                EditorUtility.DisplayDialog("Delete Saves", "Сейвов нет: ни файла SDK, ни ключа в PlayerPrefs.", "OK");
                return;
            }
            string what = (sdk ? "файл SDK\n" + SdkSavePath + "\n" : "") + (prefs ? "ключи PlayerPrefs (сейв и настройки)" : "");
            if (!EditorUtility.DisplayDialog("Delete Saves", "Удалить сейвы и настройки?\n\n" + what + "\n\nКопия будет положена в Tools/backups/.", "Удалить", "Отмена")) return;

            string stamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string dir = Path.Combine(Directory.GetCurrentDirectory(), "Tools", "backups");
            Directory.CreateDirectory(dir);
            var log = "SortThem: saves deleted; ";
            if (sdk)
            {
                string backup = Path.Combine(dir, "SortThem_sdk_" + stamp + ".txt");
                File.Copy(SdkSavePath, backup, true);
                File.Delete(SdkSavePath);
                log += "SDK → " + backup + "; ";
            }
            if (prefs)
            {
                string backup = Path.Combine(dir, "PlayerPrefs_save_" + stamp + ".txt");
                File.WriteAllText(backup, PlayerPrefs.GetString("sortthem.save", ""));
                foreach (var k in PrefKeys) PlayerPrefs.DeleteKey(k);
                PlayerPrefs.Save();
                log += "PlayerPrefs → " + backup;
            }
            Debug.Log(log);
        }

        [MenuItem("SortThem/Dev/Reveal SDK Save Folder")]
        public static void RevealSaveFolder()
        {
            string dir = Path.GetDirectoryName(SdkSavePath);
            if (!Directory.Exists(dir)) { Debug.Log("SortThem: папка сейвов SDK ещё не создана: " + dir); return; }
            EditorUtility.RevealInFinder(File.Exists(SdkSavePath) ? SdkSavePath : dir);
        }
    }
}

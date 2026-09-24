using System;
using System.Collections.Generic;
using UnityEngine;
using UpscaleSDK.Core.Saves.FileSystems;

namespace SortThem.Web
{
    public class WebFileSystem : IFileSystem
    {
        const string Prefix = "ups.file.";
        const string TimePrefix = "ups.time.";
        const string IndexKey = "ups.files";

        public event Action<int, string> OnReadError;
        public event Action<int, string> OnWriteError;
        public event Action<string> OnFileReadFinished;
        public event Action<FileEntry[]> OnFilesFound;

        public void Write(string fileName, string extension, string data)
        {
            string name = fileName + "." + extension;
            try
            {
                PlayerPrefs.SetString(Prefix + name, data);
                PlayerPrefs.SetString(TimePrefix + name, DateTime.Now.Ticks.ToString());
                var index = Index();
                if (!index.Contains(name))
                {
                    index.Add(name);
                    PlayerPrefs.SetString(IndexKey, string.Join("\n", index));
                }
                PlayerPrefs.Save();
            }
            catch (Exception e)
            {
                Debug.LogError("SortThem: web save failed: " + e.Message);
                OnWriteError?.Invoke(500, e.Message);
            }
        }

        public bool Read(string fileName, string extension)
        {
            string key = Prefix + fileName + "." + extension;
            if (PlayerPrefs.HasKey(key))
            {
                OnFileReadFinished?.Invoke(PlayerPrefs.GetString(key));
                return true;
            }
            OnReadError?.Invoke(404, "File not found");
            return false;
        }

        public void StartFileSearch()
        {
            var entries = new List<FileEntry>();
            foreach (var name in Index())
            {
                if (!PlayerPrefs.HasKey(Prefix + name)) continue;
                int dot = name.LastIndexOf('.');
                DateTime? modified = null;
                if (long.TryParse(PlayerPrefs.GetString(TimePrefix + name, ""), out long ticks)) modified = new DateTime(ticks);
                entries.Add(new FileEntry
                {
                    Name = dot >= 0 ? name.Substring(0, dot) : name,
                    Extension = dot >= 0 ? name.Substring(dot + 1) : string.Empty,
                    LastModified = modified,
                    IsDirectory = false
                });
            }
            OnFilesFound?.Invoke(entries.ToArray());
        }

        static List<string> Index()
        {
            var list = new List<string>();
            foreach (var s in PlayerPrefs.GetString(IndexKey, "").Split('\n'))
                if (!string.IsNullOrEmpty(s)) list.Add(s);
            return list;
        }
    }
}

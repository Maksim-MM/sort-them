using System;
using System.Collections.Generic;
using Playgama;
using UnityEngine;
using UpscaleSDK.Core.Saves.FileSystems;

namespace SortThem.Web
{
    public class WebFileSystem : IFileSystem
    {
        const string Prefix = "ups.file.";
        const string IndexKey = "ups.files";

        public event Action<int, string> OnReadError;
        public event Action<int, string> OnWriteError;
        public event Action<string> OnFileReadFinished;
        public event Action<FileEntry[]> OnFilesFound;

        List<string> _index = new List<string>();

        public void Write(string fileName, string extension, string data)
        {
            string name = fileName + "." + extension;
            if (!_index.Contains(name)) _index.Add(name);
            var keys = new List<string> { Prefix + name, IndexKey };
            var values = new List<object> { data, string.Join("\n", _index) };
            Bridge.storage.Set(keys, values, ok =>
            {
                if (ok) return;
                Debug.LogError("SortThem: web save failed: storage set rejected");
                OnWriteError?.Invoke(500, "storage set rejected");
            });
        }

        public bool Read(string fileName, string extension)
        {
            Bridge.storage.Get(Prefix + fileName + "." + extension, (ok, data) =>
            {
                if (!ok)
                {
                    Debug.LogError("SortThem: web save read failed: storage get rejected");
                    return;
                }
                if (string.IsNullOrEmpty(data)) OnReadError?.Invoke(404, "File not found");
                else OnFileReadFinished?.Invoke(data);
            });
            return true;
        }

        public void StartFileSearch()
        {
            Bridge.storage.Get(IndexKey, (ok, data) =>
            {
                if (!ok)
                {
                    Debug.LogError("SortThem: web save index read failed: storage get rejected");
                    return;
                }
                _index = new List<string>();
                var entries = new List<FileEntry>();
                if (!string.IsNullOrEmpty(data))
                {
                    foreach (var name in data.Split('\n'))
                    {
                        if (string.IsNullOrEmpty(name)) continue;
                        _index.Add(name);
                        int dot = name.LastIndexOf('.');
                        entries.Add(new FileEntry
                        {
                            Name = dot >= 0 ? name.Substring(0, dot) : name,
                            Extension = dot >= 0 ? name.Substring(dot + 1) : string.Empty,
                            IsDirectory = false
                        });
                    }
                }
                OnFilesFound?.Invoke(entries.ToArray());
            });
        }
    }
}

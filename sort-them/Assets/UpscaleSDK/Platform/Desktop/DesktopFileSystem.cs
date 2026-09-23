using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UpscaleSDK.Core.Saves.FileSystems;
using UpscaleSDK.Core.Utils;

namespace UpscaleSDK.Platform.Desktop
{
    /// <summary>
    /// Represents a desktop file system class.
    /// </summary>
    public class DesktopFileSystem : IFileSystem
    {
        private const string DirectoryName = "UpscaleSaves";

        /// <summary>
        /// Occurs when read error.
        /// </summary>
        public event Action<int, string> OnReadError;
        /// <summary>
        /// Occurs when write error.
        /// </summary>
        public event Action<int, string> OnWriteError;
        /// <summary>
        /// Occurs when file read finished.
        /// </summary>
        public event Action<string> OnFileReadFinished;
        /// <summary>
        /// Occurs when files found.
        /// </summary>
        public event Action<FileEntry[]> OnFilesFound;


        /// <summary>
        /// Write.
        /// </summary>
        /// <param name="fileName">The file name.</param>
        /// <param name="extension">The extension.</param>
        /// <param name="data">The data.</param>
        public void Write(string fileName, string extension, string data)
        {
            string directoryPath = Path.Combine(Application.persistentDataPath, DirectoryName);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            string filePath = Path.Combine(directoryPath, $"{fileName}.{extension}");
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch (Exception e)
            {
                UPSLogger.SavesErrorLog($"Error deleting file at: {filePath}. Exception: {e.Message}");
                OnWriteError?.Invoke(500, "Error deleting existing file");
                return;
            }

            File.WriteAllText(filePath, data);
            UPSLogger.SavesInfoLog($"File saved at: {filePath}");
        }

        /// <summary>
        /// Read.
        /// </summary>
        /// <param name="fileName">The file name.</param>
        /// <param name="extension">The extension.</param>
        public bool Read(string fileName, string extension)
        {
            string filePath = Path.Combine(Application.persistentDataPath, DirectoryName, $"{fileName}.{extension}");
            if (File.Exists(filePath))
            {
                OnFileReadFinished?.Invoke(File.ReadAllText(filePath));
                return true;
            }
            UPSLogger.SavesErrorLog($"File not found at: {filePath}");
            OnReadError?.Invoke(404, "File not found");
            return false;
        }

        /// <summary>
        /// Start file search.
        /// </summary>
        public void StartFileSearch()
        {
            string directoryPath = Path.Combine(Application.persistentDataPath, DirectoryName);
            var fileEntries = new List<FileEntry>();
            if (Directory.Exists(directoryPath))
            {
                var files = Directory.GetFiles(directoryPath);
                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    fileEntries.Add(new FileEntry
                    {
                        Name = Path.GetFileNameWithoutExtension(fileInfo.Name),
                        Extension = fileInfo.Extension.TrimStart('.'),
                        LastModified = fileInfo.LastWriteTime,
                        IsDirectory = false
                    });
                }
                
                var directories = Directory.GetDirectories(directoryPath);
                foreach (var dir in directories)
                {
                    var dirInfo = new DirectoryInfo(dir);
                    fileEntries.Add(new FileEntry
                    {
                        Name = dirInfo.Name,
                        Extension = string.Empty,
                        LastModified = dirInfo.LastWriteTime,
                        IsDirectory = true
                    });
                }
            }
            OnFilesFound?.Invoke(fileEntries.ToArray());
        }
    }
}
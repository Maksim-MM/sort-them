#if UNITY_SWITCH|| UNITY_EDITOR
#define SDK_SWITCH_COMPATIBLE
#endif

using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using UpscaleSDK.Core.Saves.FileSystems;
using UpscaleSDK.Core.Saves.Settings;
#if SDK_SWITCH_COMPATIBLE
using Plugins.UpscaleSDK.Switch.Runtime.Initialization;
using nn.fs;
using UpscaleSDK.Core.Utils;
#endif

namespace Plugins.UpscaleSDK.Saves.Runtime.Switch
{
    /// <summary>
    /// Represents a switch file system class.
    /// </summary>
    public class SwitchFileSystem : IFileSystem
    {
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

        private readonly SavesSettings _savesSettings;

        public SwitchFileSystem(SavesSettings savesSettings)
        {
            _savesSettings = savesSettings;
        }
        
        /// <summary>
        /// Writes data to a file on Nintendo Switch file system.
        /// </summary>
        /// <param name="extension">Extension is ignored On Nintendo Switch</param>
        /// <param name="data">Data to write to the file.</param>
        public void Write(string fileName, string extension, string data)
        {
#if SDK_SWITCH_COMPATIBLE
            nn.account.Uid userId = default;
            userId = SwitchPlatform.UserId;
            bool fileHandleCreated = false;
            FileHandle handle = default;
#if UNITY_SWITCH
            UnityEngine.Switch.Notification.EnterExitRequestHandlingSection();
#endif
            try
            {
                var mountResult = Mount(userId);
                mountResult.abortUnlessSuccess();
                nn.Result result;
                string filePath = string.Format("{0}:/{1}", _savesSettings.ProjectFolderName, fileName);
                byte[] dataBytes = Encoding.UTF8.GetBytes(data);
                handle = new FileHandle();
                fileHandleCreated = true;
                while (true)
                {
                    result = File.Open(ref handle, filePath, OpenFileMode.Write);
                    if (result.IsSuccess())
                    {
                        fileHandleCreated = true;
                        break;
                    }
                    else
                    {
                        if (FileSystem.ResultPathNotFound.Includes(result))
                        {
                            result = File.Create(filePath, dataBytes.LongLength);
                            if (!result.IsSuccess())
                            {
                                UPSLogger.SavesErrorLog($"Failed to create {filePath}: {result.ToString()}");
                                return;
                            }

                            fileHandleCreated = true;
                        }
                        else
                        {
                            UPSLogger.SavesErrorLog($"Failed to open {filePath}: {result.ToString()}");
                            return;
                        }
                    }
                }

                result = File.SetSize(handle, dataBytes.LongLength);
                if (FileSystem.ResultUsableSpaceNotEnough.Includes(result))
                {
                    UPSLogger.SavesErrorLog($"Insufficient space to write {dataBytes.LongLength} bytes to {filePath}");
                    File.Close(handle);
                    result.abortUnlessSuccess();
                    return;
                }

                result = File.Write(handle, 0, dataBytes, dataBytes.LongLength, WriteOption.Flush);
                if (FileSystem.ResultUsableSpaceNotEnough.Includes(result))
                {
                    UPSLogger.SavesErrorLog($"Insufficient space to write {dataBytes.LongLength} bytes to {filePath}");
                    result.abortUnlessSuccess();
                }
            }
            catch (Exception e)
            {
                UPSLogger.SavesErrorLog(e.Message);
            }
            finally
            {
                if (fileHandleCreated)
                {
                    File.Close(handle);
                }

                FileSystem.Commit(_savesSettings.ProjectFolderName);
                Unmount();
#if UNITY_SWITCH
                UnityEngine.Switch.Notification.LeaveExitRequestHandlingSection();
#endif
            }
#endif
        }

        /// <summary>
        /// Reads data from a file on Nintendo Switch file system.
        /// </summary>
        /// <param name="extension">Extension is ignored On Nintendo Switch</param>
        /// <returns>True if the file was read successfully, false otherwise.</returns>
        public bool Read(string fileName, string extension)
        {
#if SDK_SWITCH_COMPATIBLE
            nn.account.Uid userId = default;
            userId = SwitchPlatform.UserId;
            bool fileHandleCreated = false;
            FileHandle handle = default;
            try
            {
                var mountResult = Mount(userId);
                mountResult.abortUnlessSuccess();
                nn.Result result;
                string filePath = string.Format("{0}:/{1}", _savesSettings.ProjectFolderName, fileName);
                handle = new FileHandle();
                result = File.Open(ref handle, filePath, OpenFileMode.Read);
                if (!result.IsSuccess())
                {
                    if (FileSystem.ResultPathNotFound.Includes(result))
                    {
                        UPSLogger.SavesErrorLog($"File not found: {filePath}");
                        return false;
                    }
                    else
                    {
                        UPSLogger.SavesErrorLog($"Unable to open {filePath}: {result.ToString()}");
                        return false;
                    }
                }

                fileHandleCreated = true;
                long fileSize = 0;
                File.GetSize(ref fileSize, handle);
                byte[] dataBytes = new byte[fileSize];
                File.Read(handle, 0, dataBytes, fileSize);
                string data = Encoding.UTF8.GetString(dataBytes);
                OnFileReadFinished?.Invoke(data);
                return true;
            }
            catch (Exception e)
            {
                UPSLogger.SavesErrorLog(e.Message);
            }
            finally
            {
                if (fileHandleCreated)
                {
                    File.Close(handle);
                }

                Unmount();
            }
#endif
            return false;
        }

        /// <summary>
        /// Starts searching for files on Nintendo Switch file system. When files are found, the OnFilesFound event is invoked.
        /// </summary>
        public void StartFileSearch()
        {
#if SDK_SWITCH_COMPATIBLE
            nn.account.Uid userId = default;
            userId = SwitchPlatform.UserId;
            DirectoryHandle dHandle = new();
            bool directoryIsOpen = false;
            try
            {
                var mountResult = Mount(userId);
                mountResult.abortUnlessSuccess();
                nn.Result result;
                result = Directory.Open(ref dHandle, $"{_savesSettings.ProjectFolderName}:/", OpenDirectoryMode.File);
                if (result.IsSuccess() == false)
                {
                    UPSLogger.SavesErrorLog(result.ToString());
                    return;
                }

                directoryIsOpen = true;
                long entryCount = 0;
                result = Directory.GetEntryCount(ref entryCount, dHandle);
                DirectoryEntry[] entries = new DirectoryEntry[entryCount];
                long actualEntryCount = 0;
                result = Directory.Read(ref actualEntryCount, entries, dHandle, entryCount);
                if (result.IsSuccess() == false)
                {
                    UPSLogger.SavesErrorLog(result.ToString());
                    Directory.Close(dHandle);
                    directoryIsOpen = false;
                    return;
                }

                List<FileEntry> files = new List<FileEntry>();
                for (int i = 0; i < actualEntryCount; i++)
                {
                    DirectoryEntry directoryEntry = entries[i];
                    if (directoryEntry.entryType == EntryType.Directory) continue; // Skip directories
                    FileEntry file = new FileEntry();
                    file.Name = directoryEntry.name;
                    file.IsSave = true;
                    file.Extension = null;
                    file.LastModified = null;
                    files.Add(file);
                }

                OnFilesFound?.Invoke(files.ToArray());
            }
            catch (Exception e)
            {
                UPSLogger.SavesErrorLog(e.Message);
            }
            finally
            {
                if (directoryIsOpen) Directory.Close(dHandle);
                Unmount();
            }
#endif
        }

#if SDK_SWITCH_COMPATIBLE
        private nn.Result Mount(nn.account.Uid userId)
        {
            return SaveData.Mount(_savesSettings.ProjectFolderName, userId);
        }

        private void Unmount()
        {
            FileSystem.Unmount(_savesSettings.ProjectFolderName);
        }
#endif
    }
}
#if UNITY_GAMECORE_XBOXSERIES || UNITY_GAMECORE_XBOXONE
#define SUPPORT_XBOX
#endif
using System;
using System.Collections.Generic;
using UpscaleSDK.Core.Saves.FileSystems;
using UpscaleSDK.Core.Saves.Settings;
#if SUPPORT_XBOX
using Unity.XGamingRuntime;
#endif

namespace UpscaleSDK.Platform.Xbox
{
    /// <summary>
    /// Represents a xbox file system class.
    /// </summary>
    public class XboxFileSystem : IFileSystem
    {
        /// <summary>
        /// Occurs when write error.
        /// </summary>
        public event Action<int, string> OnWriteError;
        /// <summary>
        /// Occurs when read error.
        /// </summary>
        public event Action<int, string> OnReadError;
        /// <summary>
        /// Occurs when file read finished.
        /// </summary>
        public event Action<string> OnFileReadFinished;
        /// <summary>
        /// Occurs when files found.
        /// </summary>
        public event Action<FileEntry[]> OnFilesFound;
        
        private readonly SavesSettings _settings;

        public XboxFileSystem(SavesSettings settings)
        {
            _settings = settings;
        }

        /// <summary>
        /// Write.
        /// </summary>
        /// <param name="fileName">The file name.</param>
        /// <param name="extension">The extension.</param>
        /// <param name="data">The data.</param>
        public void Write(string fileName, string extension, string data)
        {
#if SUPPORT_XBOX
            int hresult = SDK.XGameSaveCreateContainer(XboxPlatform.GetData().ProviderHandle,
                _settings.XboxContainerName, out var containerHandle);
            if (HR.FAILED(hresult))
            {
                OnWriteError?.Invoke(hresult, String.Empty);
                return;
            }
            hresult = SDK.XGameSaveCreateUpdate(containerHandle, 
                _settings.XboxContainerName, out var updateHandle);
            if (HR.FAILED(hresult))
            {
                OnWriteError?.Invoke(hresult, String.Empty);
                return;
            }

            byte[] dataBytes = System.Text.Encoding.UTF8.GetBytes(data); // Assuming UTF8 encoding
            if (updateHandle == null)
            {
                OnWriteError?.Invoke(hresult, String.Empty);
                return;
            }

            hresult = SDK.XGameSaveSubmitBlobWrite(updateHandle, fileName, dataBytes);
            if (HR.FAILED(hresult))
            {
                OnWriteError?.Invoke(hresult, fileName);
                return;
            }

            hresult = SDK.XGameSaveSubmitUpdateAsync(updateHandle,
                (hresult) => OnSubmitUpdateCompleted(hresult, fileName, updateHandle, containerHandle));
#endif
        }

        /// <summary>
        /// Read.
        /// </summary>
        /// <param name="fileName">The file name.</param>
        /// <param name="extension">The extension.</param>
        public bool Read(string fileName, string extension)
        {
#if SUPPORT_XBOX
            XGameSaveContainerHandle containerHandle;
            Int32 hresult = SDK.XGameSaveCreateContainer(XboxPlatform.GetData().ProviderHandle, _settings.XboxContainerName, out containerHandle);
            if (HR.FAILED(hresult))
            {
                OnReadError?.Invoke(hresult, String.Empty);
                return false;
            }
            hresult = SDK.XGameSaveReadBlobDataAsync(containerHandle,
                new[] { fileName }, (hresult, blobs)
                    => OnGameSaveReadBlobDataCompleted(hresult, blobs, fileName, containerHandle));
            return HR.SUCCEEDED(hresult);
#else
            return false;
#endif
        }

        /// <summary>
        /// Start file search.
        /// </summary>
        public void StartFileSearch()
        {
#if SUPPORT_XBOX
            List<FileEntry> files = new List<FileEntry>();
            
            var hr = SDK.XGameSaveCreateContainer(XboxPlatform.GetData().ProviderHandle,
                _settings.XboxContainerName, out var containerHandle);
            if (HR.FAILED(hr))
            {
                OnFilesFound?.Invoke(files.ToArray());
                return;
            }
            XGameSaveBlobInfo[] blobInfos;
            hr = SDK.XGameSaveEnumerateBlobInfo(containerHandle, out blobInfos);
            Dictionary<string, UInt32> blobInfosDict = new Dictionary<string, UInt32>();
            if (HR.SUCCEEDED(hr))
            {
                for (int i = 0; i < blobInfos.Length; i++)
                {
                    blobInfosDict.Add(blobInfos[i].Name, blobInfos[i].Size);
                }
            }
            
            foreach (var blobInfo in blobInfosDict)
            {
                files.Add(new FileEntry()
                {
                    Name = blobInfo.Key,
                    IsDirectory = false,
                    IsSave = true,
                });
            }
            
            OnFilesFound?.Invoke(files.ToArray());
            SDK.XGameSaveCloseContainer(containerHandle);
#endif
        }

#if SUPPORT_XBOX
        private void OnSubmitUpdateCompleted(int hresult, string fileName, XGameSaveUpdateHandle updateHandle,
            XGameSaveContainerHandle containerContext)
        {
            if (HR.FAILED(hresult))
            {
                OnWriteError?.Invoke(hresult, fileName);
                return;
            }

            SDK.XGameSaveCloseUpdate(updateHandle);
            SDK.XGameSaveCloseContainer(containerContext);
        }

        private void OnGameSaveReadBlobDataCompleted(int hresult, XGameSaveBlob[] blobs, string fileName,
            XGameSaveContainerHandle containerHandle)
        {
            if (HR.FAILED(hresult))
            {
                OnReadError?.Invoke(hresult, fileName);
                SDK.XGameSaveCloseContainer(containerHandle);
                return;
            }

            foreach (var blob in blobs)
            {
                string content = System.Text.Encoding.UTF8.GetString(blob.Data); // Assuming UTF8 encoding
                OnFileReadFinished?.Invoke(content);
            } 
            SDK.XGameSaveCloseContainer(containerHandle);
        }
#endif
    }
}
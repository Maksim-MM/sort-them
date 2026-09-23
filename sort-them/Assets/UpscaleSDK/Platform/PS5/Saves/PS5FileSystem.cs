using System;
using System.Collections.Generic;
using Plugins.UpscaleSDK.PS5.Runtime.Users;
using UnityEngine;
using Plugins.UpscaleSDK.PS5.Runtime.DependencyManagement;
using System.Collections;
using Plugins.UpscaleSDK.PS5.Runtime.Common;
using UpscaleSDK.Core.Saves.FileSystems;
using UpscaleSDK.Core.Saves.Settings;
using UpscaleSDK.Core.Saves.ErrorHandling;


#if UNITY_PS5
using Unity.SaveData.PS5.Dialog;
using Unity.SaveData.PS5.Search;
using DirName = Unity.SaveData.PS5.Core.DirName;
using EmptyResponse = Unity.SaveData.PS5.Core.EmptyResponse;
using FileOps = Unity.SaveData.PS5.Info.FileOps;
using FunctionTypes = Unity.SaveData.PS5.Core.FunctionTypes;
using Main = Unity.SaveData.PS5.Main;
using Mounting = Unity.SaveData.PS5.Mount.Mounting;
using ReturnCodes = Unity.SaveData.PS5.Core.ReturnCodes;
using SaveDataCallbackEvent = Unity.SaveData.PS5.Core.SaveDataCallbackEvent;
using SaveDataException = Unity.SaveData.PS5.Core.SaveDataException;
using SaveDataParams = Unity.SaveData.PS5.Info.SaveDataParams;
#endif

namespace Plugins.UpscaleSDK.Saves.Runtime.PS5
{
    /// <summary>
    /// Represents a ps5 file system class.
    /// </summary>
    public class PS5FileSystem : IFileSystem
    {
#if UNITY_PS5
        // 40 mb default size
        private UInt64 DefaultSize =>
            Mounting.MountRequest.BLOCKS_MIN + ((1024 * 1024 * _savesSettings.SaveSize) / Mounting.MountRequest.BLOCK_SIZE);
#endif
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

        private readonly MonoBehaviour _monoBeh;
        private readonly SavesSettings _savesSettings;
        private UserData _userData;
        private int savesIndex = 0;

        public PS5FileSystem(MonoBehaviour monoBeh, SavesSettings savesSettings)
        {
            _userData = IDependency<UserData>.Instance;
#if UNITY_PS5
            Main.OnAsyncEvent += AsyncEventCallback;
            _monoBeh = monoBeh;
            _savesSettings = savesSettings;
#endif
        }


        /// <summary>
        /// Writes data to a file in the PS5 save data system.
        /// </summary>
        /// <param name="data">The string data to be written to the file.</param>
        public void Write(string fileName, string extension, string data)
        {
#if UNITY_PS5
            try
            {
                if (_userData == null) _userData = IDependency<UserData>.Instance;
                _monoBeh.StartCoroutine(WriteCoroutine(fileName, _savesSettings.ProjectFolderName, extension, data));
            }
            catch (SaveDataException ex)
            {
                Debug.LogError($"PS5 Write operation failed with error: {ex.ExtendedMessage}");
            }
#endif
        }

        /// <summary>
        /// Reads data from a file in the PS5 save data system. When the read operation is complete,
        /// the OnFileReadFinished event will be invoked with the file content.
        /// </summary>
        /// <param name="fileName">The file name.</param>
        /// <param name="extension">The extension.</param>
        /// <returns>True if the read operation was initiated successfully; otherwise, false.</returns>
        public bool Read(string fileName, string extension)
        {
#if UNITY_PS5
            if (_userData == null) _userData = IDependency<UserData>.Instance;
            try
            {
                ReadFileRequest fileRequest = new ReadFileRequest(fileName, extension, _savesSettings.ProjectFolderName);
                fileRequest.IgnoreCallback = false;
                ReadFileResponse fileResponse = new ReadFileResponse();
                DirName dirName = new DirName();
                dirName.Data = _savesSettings.ProjectFolderName;
                _monoBeh.StartCoroutine(ReadCoroutine(_userData.PSUser.userId, dirName, fileRequest, fileResponse));
            }
            catch (SaveDataException ex)
            {
                Debug.LogError($"PS5 Read operation failed with error: {ex.ExtendedMessage}");
                return false;
            }

            return true;
#else
            return false;
#endif
        }
        
        /// <summary>
        /// Starts a search for files in the PS5 save data system. When the search is complete,
        /// the OnFilesFound event will be invoked with the found files.
        /// </summary>
        public void StartFileSearch()
        {
#if UNITY_PS5
            if (_userData == null) _userData = IDependency<UserData>.Instance;
            try
            {
                Searching.DirNameSearchRequest req = new Searching.DirNameSearchRequest();
                req.UserId = _userData.PSUser.userId;
                req.Key = Searching.SearchSortKey.DirName;
                req.Order = Searching.SearchSortOrder.Ascending;
                req.IncludeBlockInfo = true;
                req.IncludeParams = true;
                req.MaxDirNameCount = Searching.DirNameSearchRequest.DIR_NAME_MAXSIZE;
                Searching.DirNameSearchResponse resp = new Searching.DirNameSearchResponse();
                int requestId = Searching.DirNameSearch(req, resp);
                Debug.Log("DirNameSearch Async : Request Id = " + requestId);
            }
            catch (SaveDataException ex)
            {
                Debug.LogError($"PS5 Search operation failed with error: {ex.ExtendedMessage}");
            }
#endif
        }

#if UNITY_PS5
        private IEnumerator WriteCoroutine(string fileName, string directoryName,
            string extension, string data)
        {
            savesIndex += 1;
            Dialogs.NewItem newItem = new();
            newItem.IconPath = "/app0/Media/StreamingAssets/PS5UpscaleSaveIcon.png";
            newItem.Title = fileName;
            DirName dirName = new DirName();
            dirName.Data = directoryName;
            bool backup = true;
            UInt64 blocksSize = DefaultSize;
            SaveDataParams saveDataParams = new();
            saveDataParams.Title = newItem.Title;
            saveDataParams.SubTitle = fileName;
            saveDataParams.Detail = "Saved data";
            saveDataParams.UserParam = (uint)savesIndex;
            SaveState currentState = SaveState.Begin;
            Mounting.MountResponse mountResponse = new Mounting.MountResponse();
            Mounting.MountPoint mp = null;
            int errorCode = 0;

            var fileRequest = new WriteFileRequest(fileName, extension, directoryName, data);
            fileRequest.IgnoreCallback = false;
            var fileResponse = new WriteFileResponse();
            while (currentState != SaveState.Exit)
            {
                switch (currentState)
                {
                    case SaveState.Begin:
                    {
                        Mounting.MountModeFlags flags = Mounting.MountModeFlags.Create2 |
                                                        Mounting.MountModeFlags.ReadWrite;

                        errorCode = MountSaveData(_userData.PSUser.userId, blocksSize, mountResponse, dirName, flags);

                        if (errorCode < 0)
                        {
                            currentState = SaveState.HandleError;
                        }
                        else
                        {
                            // Wait for save data to be mounted.
                            while (mountResponse.Locked == true)
                            {
                                yield return null;
                            }

                            if (mountResponse.IsErrorCode == true)
                            {
                                errorCode = mountResponse.ReturnCodeValue;

                                // Must handle no space and broken save games
                                //    ReturnCodes.DATA_ERROR_NO_SPACE_FS
                                //    ReturnCodes.SAVE_DATA_ERROR_BROKEN)
                                currentState = SaveState.HandleError;
                            }
                            else
                            {
                                // Save data is now mounted, so files can be saved.
                                mp = mountResponse.MountPoint;
                                currentState = SaveState.SaveFiles;
                            }
                        }
                    }
                        break;
                    case SaveState.SaveFiles:
                    {
                        // Do actual saving
                        fileRequest.MountPointName = mp.PathName;
                        fileRequest.Async = true;
                        fileRequest.UserId = _userData.PSUser.userId;

                        errorCode = FileOps.CustomFileOp(fileRequest, fileResponse);

                        if (errorCode < 0)
                        {
                            currentState = SaveState.HandleError;
                        }
                        else
                        {
                            while (fileResponse.Locked == true)
                            {
                                yield return null;
                            }

                            // Write the icon and any detail parmas set here.
                            EmptyResponse iconResponse = new EmptyResponse();

                            errorCode = WriteIcon(_userData.PSUser.userId, iconResponse, mp, newItem);

                            if (errorCode < 0)
                            {
                                currentState = SaveState.HandleError;
                            }
                            else
                            {
                                EmptyResponse paramsResponse = new EmptyResponse();

                                errorCode = WriteParams(_userData.PSUser.userId, paramsResponse, mp, saveDataParams);

                                if (errorCode < 0)
                                {
                                    currentState = SaveState.HandleError;
                                }
                                else
                                {
                                    // Wait for save icon to be mounted.
                                    while (iconResponse.Locked == true || paramsResponse.Locked == true)
                                    {
                                        yield return null;
                                    }

                                    currentState = SaveState.WriteIcon;
                                }
                            }
                        }
                    }
                        break;
                    case SaveState.WriteIcon:
                    {
                        // Write the icon and any detail parmas set here.
                        EmptyResponse iconResponse = new EmptyResponse();

                        errorCode = WriteIcon(_userData.PSUser.userId, iconResponse, mp, newItem);

                        if (errorCode < 0)
                        {
                            currentState = SaveState.HandleError;
                        }
                        else
                        {
                            while (iconResponse.Locked == true)
                            {
                                yield return null;
                            }

                            currentState = SaveState.WriteParams;
                        }
                    }
                        break;
                    case SaveState.WriteParams:
                    {
                        EmptyResponse paramsResponse = new EmptyResponse();

                        errorCode = WriteParams(_userData.PSUser.userId, paramsResponse, mp, saveDataParams);

                        if (errorCode < 0)
                        {
                            currentState = SaveState.HandleError;
                        }
                        else
                        {
                            // Wait for save icon to be mounted.
                            while (paramsResponse.Locked == true)
                            {
                                yield return null;
                            }

                            currentState = SaveState.Unmount;
                        }
                    }
                        break;
                    case SaveState.Unmount:
                    {
                        EmptyResponse unmountResponse = new EmptyResponse();

                        errorCode = UnmountSaveData(_userData.PSUser.userId, unmountResponse, mp);

                        if (errorCode < 0)
                        {
                            currentState = SaveState.HandleError;
                        }
                        else
                        {
                            while (unmountResponse.Locked == true)
                            {
                                yield return null;
                            }

                            currentState = SaveState.Exit;
                        }
                    }
                        break;
                    case SaveState.HandleError:
                    {
                        if (mp != null)
                        {
                            EmptyResponse unmountResponse = new EmptyResponse();

                            UnmountSaveData(_userData.PSUser.userId, unmountResponse, mp);
                        }


                        if (errorCode == -2137063414) // ReturnCodes.DATA_ERROR_NO_SPACE_FS
                        {
                            Dialogs.SystemMessageParam msgParam = new();
                            msgParam.SysMsgType = Dialogs.SystemMessageType.NoSpaceContinuable;
                            msgParam.Value = 96;
                            Dialogs.OpenDialogRequest request = new();
                            request.UserId = _userData.PSUser.userId;
                            request.Async = true;
                            request.Mode = Dialogs.DialogMode.SystemMsg;
                            request.DispType = Dialogs.DialogType.Save;
                            request.SystemMessage = msgParam;
                            request.Animations = new Dialogs.AnimationParam(Dialogs.Animation.On, Dialogs.Animation.On);
                            request.Option = new Dialogs.OptionParam() { Back = Dialogs.OptionBack.Disable };
                            Dialogs.OpenDialogResponse response = new();
                            Dialogs.OpenDialog(request, response);

                            ErrorHandler.RaiseError("Save failed: Not enough space available.");

                            Debug.Log("PS5 SAVE ERROR FOUND: DATA_ERROR_NO_SPACE_FS. TYPE: WRITE");
                        }
                        else if (errorCode == -2137063409) // ReturnCodes.SAVE_DATA_ERROR_BROKEN
                        {
                            var request = new Dialogs.OpenDialogRequest();
                            var response = new Dialogs.OpenDialogResponse();
                            request.UserId = _userData.PSUser.userId;
                            request.Mode = Dialogs.DialogMode.SystemMsg;
                            request.DispType = Dialogs.DialogType.Save;
                            request.SystemMessage = new Dialogs.SystemMessageParam()
                            {
                                SysMsgType = Dialogs.SystemMessageType.Corrupted,
                            };
                            request.Animations = new Dialogs.AnimationParam(Dialogs.Animation.On, Dialogs.Animation.On);
                            DirName[] dirNames = new DirName[1];
                            dirNames[0] = dirName;
                            Dialogs.Items items = new Dialogs.Items();
                            items.DirNames = dirNames;
                            request.Items = items;
                            Dialogs.NewItem brokenItem = new Dialogs.NewItem();
                            brokenItem.Title = fileName;
                            brokenItem.IconPath = newItem.IconPath;
                            request.NewItem = brokenItem;
                            request.IgnoreCallback = true;
                            Dialogs.OpenDialog(request, response);

                            ErrorHandler.RaiseError("Save failed: The save data is corrupt.");

                            Debug.Log("PS5 SAVE ERROR FOUND: SAVE_DATA_ERROR_BROKEN. TYPE: WRITE");
                        }
                    }
                    currentState = SaveState.Exit;
                    break;
                }

                yield return null;
            }
        }

        /// <summary>
        /// Read coroutine.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="dirName">The dir name.</param>
        /// <param name="fileRequest">The file request.</param>
        /// <param name="fileResponse">The file response.</param>
        public static IEnumerator ReadCoroutine(int userId, DirName dirName,
            FileOps.FileOperationRequest fileRequest, FileOps.FileOperationResponse fileResponse)
        {
            SaveState currentState = SaveState.Begin;

            Mounting.MountResponse mountResponse = new Mounting.MountResponse();
            Mounting.MountPoint mp = null;

            int errorCode = 0;

            while (currentState != SaveState.Exit)
            {
                switch (currentState)
                {
                    case SaveState.Begin:
                    {
                        Mounting.MountModeFlags flags = Mounting.MountModeFlags.ReadOnly;

                        errorCode = MountSaveData(userId, 0, mountResponse, dirName, flags);

                        if (errorCode < 0)
                        {
                            currentState = SaveState.HandleError;
                        }
                        else
                        {
                            // Wait for save data to be mounted.
                            while (mountResponse.Locked == true)
                            {
                                yield return null;
                            }

                            if (mountResponse.IsErrorCode == true)
                            {
                                errorCode = mountResponse.ReturnCodeValue;
                                // Must handle broken save games
                                //    ReturnCodes.SAVE_DATA_ERROR_BROKEN)
                                currentState = SaveState.HandleError;
                            }
                            else
                            {
                                // Save data is now mounted, so files can be saved.
                                mp = mountResponse.MountPoint;
                                currentState = SaveState.LoadFiles;
                            }
                        }
                    }
                        break;
                    case SaveState.LoadFiles:
                    {
                        // Do actual loading
                        fileRequest.MountPointName = mp.PathName;
                        fileRequest.Async = true;
                        fileRequest.UserId = userId;

                        errorCode = FileOps.CustomFileOp(fileRequest, fileResponse);

                        if (errorCode < 0)
                        {
                            currentState = SaveState.HandleError;
                        }
                        else
                        {
                            while (fileResponse.Locked == true)
                            {
                                yield return null;
                            }

                            currentState = SaveState.Unmount;
                        }
                    }
                        break;
                    case SaveState.Unmount:
                    {
                        EmptyResponse unmountResponse = new EmptyResponse();

                        errorCode = UnmountSaveData(userId, unmountResponse, mp);

                        if (errorCode < 0)
                        {
                            currentState = SaveState.HandleError;
                        }
                        else
                        {
                            while (unmountResponse.Locked == true)
                            {
                                yield return null;
                            }

                            currentState = SaveState.Exit;
                        }
                    }
                        break;
                    case SaveState.HandleError:
                    {
                        if (mp != null)
                        {
                            EmptyResponse unmountResponse = new EmptyResponse();

                            UnmountSaveData(userId, unmountResponse, mp);
                        }
                        if (errorCode == -2137063409) // ReturnCodes.SAVE_DATA_ERROR_BROKEN
                        {
                            var request = new Dialogs.OpenDialogRequest();
                            var response = new Dialogs.OpenDialogResponse();
                            request.UserId = userId;
                            request.Mode = Dialogs.DialogMode.SystemMsg;
                            request.DispType = Dialogs.DialogType.Load;
                            request.SystemMessage = new Dialogs.SystemMessageParam()
                            {
                                SysMsgType = Dialogs.SystemMessageType.Corrupted,
                            };
                            request.Animations = new Dialogs.AnimationParam(Dialogs.Animation.On, Dialogs.Animation.On);
                            DirName[] dirNames = new DirName[1];
                            dirNames[0] = dirName;
                            Dialogs.Items items = new Dialogs.Items();
                            items.DirNames = dirNames;
                            request.Items = items;
                            Dialogs.NewItem newItem = new Dialogs.NewItem();
                            newItem.Title = dirName.Data;
                            request.NewItem = newItem;
                            request.IgnoreCallback = true;
                            Dialogs.OpenDialog(request, response);

                            ErrorHandler.RaiseError("Load failed: The save data is corrupt.");

                            Debug.Log("PS5 SAVE ERROR FOUND: SAVE_DATA_ERROR_BROKEN. TYPE: READ");
                        }
                    }
                        currentState = SaveState.Exit;
                        break;
                }

                yield return null;
            }
        }

        private void AsyncEventCallback(SaveDataCallbackEvent npEvent)
        {
            switch (npEvent.ApiCalled)
            {
                case FunctionTypes.FileOps:
                {
                    if (npEvent.Request is WriteFileRequest)
                    {
                        HandleWriteResponse(npEvent.Response as WriteFileResponse);
                    }
                    else if (npEvent.Request is ReadFileRequest)
                    {
                        HandleReadResponse(npEvent.Response as ReadFileResponse);
                    }

                    break;
                }
                case FunctionTypes.DirNameSearch:
                {
                    if (npEvent.Request is Searching.DirNameSearchRequest)
                    {
                        HandleSearchResponse(npEvent.Response as Searching.DirNameSearchResponse);
                    }

                    break;
                }
            }
        }

        internal static int MountSaveData(int userId, UInt64 blocks, Mounting.MountResponse mountResponse,
            DirName dirName, Mounting.MountModeFlags flags)
        {
            int errorCode = unchecked((int)0x80B8000E);

            try
            {
                Mounting.MountRequest request = new Mounting.MountRequest();

                request.UserId = userId;
                request.IgnoreCallback = true;
                request.DirName = dirName;

                request.MountMode = flags;

                if (blocks < Mounting.MountRequest.BLOCKS_MIN)
                {
                    blocks = Mounting.MountRequest.BLOCKS_MIN;
                }

                request.Blocks = blocks;

//              request.SystemBlocks = 0;     // setting to zero specifies savedata that does no support rollback. https://game.develop.playstation.net/resources/documents/sdk/latest/SaveData-Reference/0011.html

                Mounting.Mount(request, mountResponse);
                errorCode = 0;
            }
            catch
            {
                if (mountResponse.ReturnCodeValue < 0)
                {
                    errorCode = mountResponse.ReturnCodeValue;
                }
            }

            return errorCode;
        }

        internal static int UnmountSaveData(int userId, EmptyResponse unmountResponse, Mounting.MountPoint mp)
        {
            int errorCode = unchecked((int)0x80B8000E);

            try
            {
                Mounting.UnmountRequest request = new Mounting.UnmountRequest();

                request.UserId = userId;
                request.MountPointName = mp.PathName;
                request.IgnoreCallback = true;

                Mounting.Unmount(request, unmountResponse);

                errorCode = 0;
            }
            catch
            {
                if (unmountResponse.ReturnCodeValue < 0)
                {
                    errorCode = unmountResponse.ReturnCodeValue;
                }
            }

            return errorCode;
        }

        internal static int WriteIcon(int userId, EmptyResponse iconResponse, Mounting.MountPoint mp,
            Dialogs.NewItem newItem)
        {
            int errorCode = unchecked((int)0x80B8000E);

            try
            {
                Mounting.SaveIconRequest request = new Mounting.SaveIconRequest();

                if (mp == null) return errorCode;

                request.UserId = userId;
                request.MountPointName = mp.PathName;
                request.RawPNG = newItem.RawPNG;
                request.IconPath = newItem.IconPath;
                request.IgnoreCallback = true;

                Mounting.SaveIcon(request, iconResponse);

                errorCode = 0;
            }
            catch
            {
                if (iconResponse.ReturnCodeValue < 0)
                {
                    errorCode = iconResponse.ReturnCodeValue;
                }
            }

            return errorCode;
        }

        internal static int WriteParams(int userId, EmptyResponse paramsResponse, Mounting.MountPoint mp,
            SaveDataParams saveDataParams)
        {
            int errorCode = unchecked((int)0x80B8000E);

            try
            {
                Mounting.SetMountParamsRequest request = new Mounting.SetMountParamsRequest();

                if (mp == null) return errorCode;

                request.UserId = userId;
                request.MountPointName = mp.PathName;
                request.IgnoreCallback = true;

                request.Params = saveDataParams;

                Mounting.SetMountParams(request, paramsResponse);

                errorCode = 0;
            }
            catch
            {
                if (paramsResponse.ReturnCodeValue < 0)
                {
                    errorCode = paramsResponse.ReturnCodeValue;
                }
            }

            return errorCode;
        }


        private void HandleWriteResponse(WriteFileResponse npEventResponse)
        {
            Debug.Log($"File was written successfully. Time: {npEventResponse.WriteTime}");
        }

        private void HandleReadResponse(ReadFileResponse npEventResponse)
        {
            var content = npEventResponse.Content;

            MainThreadDispatcher.Enqueue(() =>
            {
                OnFileReadFinished?.Invoke(content);
            });
        }

        private void HandleSearchResponse(Searching.DirNameSearchResponse npEventResponse)
        {
            List<FileEntry> files = new List<FileEntry>();
            foreach (Searching.SearchSaveDataItem saveDataItem in npEventResponse.SaveDataItems)
            {
                FileEntry entry = new FileEntry();
                entry.IsDirectory = false;
                entry.Name = saveDataItem.Params.Title;
                entry.LastModified = saveDataItem.Params.Time;
                entry.Extension = null;
                entry.IsSave = true;
                files.Add(entry);
            }
            OnFilesFound?.Invoke(files.ToArray());
        }

        private enum SaveState
        {
            /// <summary>
            /// Begin.
            /// </summary>
            Begin,
            /// <summary>
            /// Save files.
            /// </summary>
            SaveFiles,
            /// <summary>
            /// Write icon.
            /// </summary>
            WriteIcon,
            /// <summary>
            /// Write params.
            /// </summary>
            WriteParams,
            /// <summary>
            /// Unmount.
            /// </summary>
            Unmount,
            /// <summary>
            /// Handle error.
            /// </summary>
            HandleError,

            /// <summary>
            /// Load files.
            /// </summary>
            LoadFiles,

            /// <summary>
            /// Exit.
            /// </summary>
            Exit
        }
#endif
    }
}
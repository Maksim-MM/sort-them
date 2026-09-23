using System;
using System.Collections.Generic;
using Plugins.UpscaleSDK.PS4.Runtime.DependencyManagement;
using Plugins.UpscaleSDK.PS4.Runtime.User;
using UnityEngine;
using UpscaleSDK.Core.Saves.FileSystems;
using UpscaleSDK.Core.Saves.Settings;
using UpscaleSDK.Core.Saves.ErrorHandling;
using System.Collections;

#if UNITY_PS4
using UnityEngine.PS4;
using Sony.PS4.SaveData;
#endif

namespace Plugins.UpscaleSDK.Saves.Runtime.PS4
{
    /// <summary>
    /// Represents a ps4 file system class.
    /// </summary>
    public class PS4FileSystem : IFileSystem
    {
#if UNITY_PS4
        private UInt64 DefaultSize =>
            Mounting.MountRequest.BLOCKS_MIN +
            ((1024 * 1024 * _savesSettings.SaveSize) / Mounting.MountRequest.BLOCK_SIZE);
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
        private int _saveIndex;

        public PS4FileSystem(MonoBehaviour monoBeh, SavesSettings savesSettings, int saveIndex = 0)
        {
#if UNITY_PS4
            _userData = IDependency<UserData>.Instance;
            _monoBeh = monoBeh;
            _savesSettings = savesSettings;
            _saveIndex = saveIndex;
            Main.OnAsyncEvent += AsyncEventHandler;
#endif
        }

        /// <summary>
        /// Writes data to a file in the PS4 save data system.
        /// </summary>
        /// <param name="data">Data that will be saved</param>
        public void Write(string fileName, string extension, string data)
        {
#if UNITY_PS4
            try
            {
                _monoBeh.StartCoroutine(WriteCoroutine(fileName, _savesSettings.ProjectFolderName, extension, data));
            }
            catch (SaveDataException e)
            {
                Debug.LogError("Exception during saving: " + e);
            }
#endif
        }

        /// <summary>
        /// Reads data from a file in the PS4 save data system. The result will be provided via the OnFileReadFinished event.
        /// </summary>
        /// <param name="fileName">The file name.</param>
        /// <param name="extension">The extension.</param>
        /// <returns>True if the read operation was initiated successfully, false otherwise.</returns>
        public bool Read(string fileName, string extension)
        {
#if UNITY_PS4
            try
            {
                if (_userData == null) _userData = IDependency<UserData>.Instance;
                if (_userData == null)
                {
                    Debug.LogError("Could not find user data!");
                    return false;
                }

                _userData.PS4User = PS4Input.RefreshUsersDetails(0);
                ReadFileRequest fileRequest = new ReadFileRequest(fileName, extension);
                fileRequest.IgnoreCallback = false;
                ReadFileResponse fileResponse = new ReadFileResponse();
                DirName dirName = new DirName();
                dirName.Data = _savesSettings.ProjectFolderName;
                _monoBeh.StartCoroutine(ReadCoroutine(_userData.PS4User.userId, dirName, fileRequest, fileResponse));
            }
            catch (SaveDataException e)
            {
                Debug.LogError("Exception during reading: " + e);
                return false;
            }

            return true;
#endif
            return false;
        }

        /// <summary>
        /// Starts a search for save files in the PS4 save data system. The results will be provided via the OnFilesFound event.
        /// </summary>
        public void StartFileSearch()
        {
#if UNITY_PS4
            if (_userData == null) _userData = IDependency<UserData>.Instance;
            try
            {
                Searching.DirNameSearchRequest request = new Searching.DirNameSearchRequest();
                request.UserId = PS4Input.RefreshUsersDetails(0).userId;
                request.Key = Searching.SearchSortKey.DirName;
                request.Order = Searching.SearchSortOrder.Ascending;
                request.IncludeBlockInfo = true;
                request.IncludeParams = true;
                request.MaxDirNameCount = Searching.DirNameSearchRequest.DIR_NAME_MAXSIZE;

                Searching.DirNameSearchResponse response = new Searching.DirNameSearchResponse();
                int requestId = Searching.DirNameSearch(request, response);
            }
            catch (SaveDataException e)
            {
                Debug.LogError("Exception during file search: " + e);
            }
#endif
        }

#if UNITY_PS4
        private IEnumerator WriteCoroutine(string fileName, string directoryName, string extension, string data)
        {
            _saveIndex += 1;
            Dialogs.NewItem newItemDialog = new();
            if (_userData == null) _userData = IDependency<UserData>.Instance;
            if (_userData == null)
            {
                Debug.LogError("Could not find user data!");
                yield return null;
            }

            _userData.PS4User = PS4Input.RefreshUsersDetails(0);
            var userId = _userData.PS4User.userId;
            newItemDialog.IconPath = "/app0/Media/StreamingAssets/PS4UpscaleSaveIcon.png";
            newItemDialog.Title = fileName;
            DirName dirName = new();
            dirName.Data = directoryName;
            bool backup = true;
            UInt64 blockSize = DefaultSize;
            SaveDataParams saveDataParams = new();
            saveDataParams.Title = newItemDialog.Title;
            saveDataParams.SubTitle = fileName;
            saveDataParams.Detail = "Saved data";
            saveDataParams.UserParam = (uint)_saveIndex;
            SaveState currentState = SaveState.Begin;
            Mounting.MountResponse mountResponse = new();
            Mounting.MountPoint mp = null;
            int errorCode = 0;

            var fileRequest = new WriteFileRequest(fileName, extension, data);
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

                        errorCode = MountSaveData(userId, blockSize, mountResponse, dirName, flags);

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
                                //    Sony.PS4.SaveData.ReturnCodes.DATA_ERROR_NO_SPACE_FS
                                //    Sony.PS4.SaveData.ReturnCodes.SAVE_DATA_ERROR_BROKEN)
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

                            // Write the icon and any detail parmas set here.
                            EmptyResponse iconResponse = new EmptyResponse();

                            errorCode = WriteIcon(userId, iconResponse, mp, newItemDialog);

                            if (errorCode < 0)
                            {
                                currentState = SaveState.HandleError;
                            }
                            else
                            {
                                EmptyResponse paramsResponse = new EmptyResponse();

                                errorCode = WriteParams(userId, paramsResponse, mp, saveDataParams);

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

                        errorCode = WriteIcon(userId, iconResponse, mp, newItemDialog);

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

                        errorCode = WriteParams(userId, paramsResponse, mp, saveDataParams);

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

                        errorCode = UnmountSaveData(userId, unmountResponse, mp, backup);

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
                        if (errorCode == -2137063414) // ReturnCodes.DATA_ERROR_NO_SPACE_FS
                        {
                            Dialogs.SystemMessageParam msgParam = new();
                            msgParam.SysMsgType = Dialogs.SystemMessageType.NoSpaceContinuable;
                            msgParam.Value = 96;
                            Dialogs.OpenDialogRequest request = new();
                            request.UserId = _userData.PS4User.userId;
                            request.Async = true;
                            request.Mode = Dialogs.DialogMode.SystemMsg;
                            request.DispType = Dialogs.DialogType.Save;
                            request.SystemMessage = msgParam;
                            request.Animations = new Dialogs.AnimationParam(Dialogs.Animation.On, Dialogs.Animation.On);
                            request.Option = new Dialogs.OptionParam() { Back = Dialogs.OptionBack.Disable };
                            Dialogs.OpenDialogResponse response = new();
                            Dialogs.OpenDialog(request, response);
                            // if (response.Result.ButtonId == Dialogs.DialogButtonIds.OK)
                            // {
                            //     Dialogs.CloseParam closeParam = new Dialogs.CloseParam();
                            //     closeParam.Anim = Dialogs.Animation.Off;
                            //     Dialogs.Close(closeParam);
                            // }
                        }

                        if (mp != null)
                        {
                            EmptyResponse unmountResponse = new EmptyResponse();
                            UnmountSaveData(userId, unmountResponse, mp, backup);
                        }

                        if (errorCode == -2137063409) // ReturnCodes.SAVE_DATA_ERROR_BROKEN
                        {
                            var request = new Dialogs.OpenDialogRequest();
                            var response = new Dialogs.OpenDialogResponse();
                            request.UserId = _userData.PS4User.userId;
                            request.Mode = Dialogs.DialogMode.SystemMsg;
                            request.DispType = Dialogs.DialogType.Save;
                            request.SystemMessage = new Dialogs.SystemMessageParam()
                            {
                                SysMsgType = Dialogs.SystemMessageType.Corrupted,
                            };
                            request.Animations =
                                new Dialogs.AnimationParam(Dialogs.Animation.On, Dialogs.Animation.On);
                            DirName[] dirNames = new DirName[1];
                            dirNames[0] = dirName;
                            Dialogs.Items items = new Dialogs.Items();
                            items.DirNames = dirNames;
                            request.Items = items;
                            Dialogs.NewItem newItem = new Dialogs.NewItem();
                            newItem.Title = fileName;
                            newItem.IconPath = newItemDialog.IconPath;
                            request.NewItem = newItem;
                            request.IgnoreCallback = true;
                            Dialogs.OpenDialog(request, response);
                            ErrorHandler.RaiseError("Save failed: The save data is corrupt.");
                        }

                        OnWriteError?.Invoke(errorCode, String.Empty);
                    }
                        currentState = SaveState.Exit;
                        break;
                }

                yield return null;
            }
        }

        private IEnumerator ReadCoroutine(int userId, DirName dirName, FileOps.FileOperationRequest fileRequest,
            FileOps.FileOperationResponse fileResponse)
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
                                //    Sony.PS4.SaveData.ReturnCodes.SAVE_DATA_ERROR_BROKEN)
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

                        errorCode = UnmountSaveData(userId, unmountResponse, mp, false);

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

                            UnmountSaveData(userId, unmountResponse, mp, false);
                        }

                        if (errorCode == -2137063409) // ReturnCodes.SAVE_DATA_ERROR_BROKEN
                        {
                            var reqeust = new Dialogs.OpenDialogRequest();
                            var response = new Dialogs.OpenDialogResponse();
                            reqeust.UserId = _userData.PS4User.userId;
                            reqeust.Mode = Dialogs.DialogMode.SystemMsg;
                            reqeust.DispType = Dialogs.DialogType.Load;
                            reqeust.SystemMessage = new Dialogs.SystemMessageParam()
                            {
                                SysMsgType = Dialogs.SystemMessageType.Corrupted,
                            };
                            reqeust.Animations =
                                new Dialogs.AnimationParam(Dialogs.Animation.On, Dialogs.Animation.On);
                            DirName[] dirNames = new DirName[1];
                            dirNames[0] = dirName;
                            Dialogs.Items items = new Dialogs.Items();
                            items.DirNames = dirNames;
                            reqeust.Items = items;
                            Dialogs.NewItem newItem = new Dialogs.NewItem();
                            newItem.Title = dirName.Data;
                            reqeust.NewItem = newItem;
                            reqeust.IgnoreCallback = true;
                            Dialogs.OpenDialog(reqeust, response);
                            ErrorHandler.RaiseError("Load failed: The save data is corrupt.");
                        }

                        OnReadError?.Invoke(errorCode, String.Empty);
                    }
                        currentState = SaveState.Exit;
                        break;
                }

                yield return null;
            }
        }

        private void AsyncEventHandler(SaveDataCallbackEvent npEvent)
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
                    HandleDirSearchResponse(npEvent.Response as Searching.DirNameSearchResponse);
                    break;
                }
            }
        }

        private void HandleDirSearchResponse(Searching.DirNameSearchResponse npEventResponse)
        {
            if (npEventResponse == null) return;

            List<FileEntry> entries = new List<FileEntry>();
            foreach (Searching.SearchSaveDataItem saveDataItem in npEventResponse.SaveDataItems)
            {
                FileEntry entry = new();
                entry.IsDirectory = false;
                entry.IsSave = true;
                entry.LastModified = saveDataItem.Params.Time;
                entry.Name = saveDataItem.Params.Title;
                entry.Extension = null;
                entries.Add(entry);
            }
            OnFilesFound?.Invoke(entries.ToArray());


        }

        private void HandleWriteResponse(WriteFileResponse npEventResponse)
        {
            Debug.Log("Write completed, total file size written: " + npEventResponse.TotalFileSizeWritten);
        }

        private void HandleReadResponse(ReadFileResponse npEventResponse)
        {
           
            var content = npEventResponse.Content;
            OnFileReadFinished?.Invoke(content);
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

        internal static int UnmountSaveData(int userId, EmptyResponse unmountResponse, Mounting.MountPoint mp,
            bool backup)
        {
            int errorCode = unchecked((int)0x80B8000E);

            try
            {
                Mounting.UnmountRequest request = new Mounting.UnmountRequest();

                request.UserId = userId;
                request.MountPointName = mp.PathName;
                request.Backup = backup;
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
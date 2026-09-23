#if UNITY_PS5
using System.IO;
using Unity.SaveData.PS5.Info;
using Unity.SaveData.PS5.Mount;

namespace Plugins.UpscaleSDK.Saves.Runtime.PS5
{
    /// <summary>
    /// Represents a read file request class.
    /// </summary>
    public class ReadFileRequest : FileOps.FileOperationRequest
    {
        private readonly string _fileName;
        private readonly string _extension;
        private readonly string _directoryName;

        public ReadFileRequest(string fileName, string extension, string directoryName)
        {
            _fileName = fileName;
            _extension = extension;
            _directoryName = directoryName;
        }


        /// <summary>
        /// Do file operations.
        /// </summary>
        /// <param name="mp">The mp.</param>
        /// <param name="response">The response.</param>
        public override void DoFileOperations(Mounting.MountPoint mp, FileOps.FileOperationResponse response)
        {
            if (mp == null) return;
            ReadFileResponse readResponse = (ReadFileResponse)response;
            string fullPath = $"{mp.PathName.Data}/{_fileName}.{_extension}";
            readResponse.Content = File.ReadAllText(fullPath);
            FileInfo info = new FileInfo(fullPath);
            readResponse.ReadTime = info.LastWriteTime;
        }
    }
}
#endif
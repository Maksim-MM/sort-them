#if UNITY_PS4
using System.IO;
using Sony.PS4.SaveData;

namespace Plugins.UpscaleSDK.Saves.Runtime.PS4
{
    /// <summary>
    /// Represents a read file request class.
    /// </summary>
    public class ReadFileRequest : FileOps.FileOperationRequest
    {
        private readonly string _fileName;
        private readonly string _extension;

        public ReadFileRequest(string fileName, string extension)
        {
            _fileName = fileName;
            _extension = extension;
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
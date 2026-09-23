#if UNITY_PS5
using System.IO;
using Unity.SaveData.PS5.Info;
using Unity.SaveData.PS5.Mount;

namespace Plugins.UpscaleSDK.Saves.Runtime.PS5
{
    /// <summary>
    /// Represents a write file request class.
    /// </summary>
    public class WriteFileRequest : FileOps.FileOperationRequest
    {
        private readonly string _fileName;
        private readonly string _extension;
        private readonly string _directoryName;
        private readonly string _content;

        public WriteFileRequest(string fileName, string extension, string directoryName, string content)
        {
            _fileName = fileName;
            _extension = extension;
            _directoryName = directoryName;
            _content = content;
        }

        /// <summary>
        /// Do file operations.
        /// </summary>
        /// <param name="mp">The mp.</param>
        /// <param name="response">The response.</param>
        public override void DoFileOperations(Mounting.MountPoint mp, FileOps.FileOperationResponse response)
        {
            if (mp == null) return;
            WriteFileResponse writeResponse = (WriteFileResponse)response;
            string fullPath = $"{mp.PathName.Data}/{_fileName}.{_extension}";
            File.WriteAllText(fullPath, _content);
            FileInfo info = new FileInfo(fullPath);
            writeResponse.WriteTime = info.LastWriteTime;
            writeResponse.TotalFileSizeWritten += info.Length;
        }
    }
}
#endif
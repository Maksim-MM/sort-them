#if UNITY_PS5
using System.IO;
using Unity.SaveData.PS5.Info;
using Unity.SaveData.PS5.Mount;

namespace Plugins.UpscaleSDK.Saves.Runtime.PS5
{
    /// <summary>
    /// Represents a enumerate files request class.
    /// </summary>
    public class EnumerateFilesRequest : FileOps.FileOperationRequest
    {
        /// <summary>
        /// Do file operations.
        /// </summary>
        /// <param name="mp">The mp.</param>
        /// <param name="response">The response.</param>
        public override void DoFileOperations(Mounting.MountPoint mp, FileOps.FileOperationResponse response)
        {
            if (mp == null) return;
            
            EnumerateFileResponse realResponse = response as EnumerateFileResponse;
            string outpath = mp.PathName.Data;
            realResponse.files = Directory.GetFiles(outpath, "*.*", SearchOption.AllDirectories);
        }
    }
}
#endif

#if UNITY_PS5
using System;
using Unity.SaveData.PS5.Info;

namespace Plugins.UpscaleSDK.Saves.Runtime.PS5
{
    /// <summary>
    /// Represents a write file response class.
    /// </summary>
    public class WriteFileResponse : FileOps.FileOperationResponse
    { 
        /// <summary>
        /// The write time field.
        /// </summary>
        public DateTime WriteTime;
        /// <summary>
        /// The total file size written field.
        /// </summary>
        public long TotalFileSizeWritten;
    }
}
#endif
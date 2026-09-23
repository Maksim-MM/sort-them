#if UNITY_PS5
using System;
using Unity.SaveData.PS5.Info;

namespace Plugins.UpscaleSDK.Saves.Runtime.PS5
{
    /// <summary>
    /// Represents a read file response class.
    /// </summary>
    public class ReadFileResponse : FileOps.FileOperationResponse
    {
        /// <summary>
        /// The content field.
        /// </summary>
        public string Content;
        /// <summary>
        /// The read time field.
        /// </summary>
        public DateTime ReadTime;
    }
}
#endif
#if UNITY_PS4
using System;
using Sony.PS4.SaveData;

namespace Plugins.UpscaleSDK.Saves.Runtime.PS4
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
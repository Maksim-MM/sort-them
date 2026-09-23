#if UNITY_PS4
using Sony.PS4.SaveData;

namespace Plugins.UpscaleSDK.Saves.Runtime.PS4
{
    /// <summary>
    /// Represents a write file response class.
    /// </summary>
    public class WriteFileResponse : FileOps.FileOperationResponse
    {
        /// <summary>
        /// The write time field.
        /// </summary>
        public System.DateTime WriteTime;
        /// <summary>
        /// The total file size written field.
        /// </summary>
        public long TotalFileSizeWritten;
    }
}
#endif
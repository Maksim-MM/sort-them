#if UNITY_PS5
using Unity.SaveData.PS5.Info;

namespace Plugins.UpscaleSDK.Saves.Runtime.PS5
{
    /// <summary>
    /// Represents a enumerate file response class.
    /// </summary>
    public class EnumerateFileResponse : FileOps.FileOperationResponse
    {
        /// <summary>
        /// The files field.
        /// </summary>
        public string[] files;
    }
}
#endif
using System;
using UnityEngine;

namespace UpscaleSDK.Core.Saves.Settings
{
    /// <summary>
    /// Represents a saves settings class.
    /// </summary>
    [CreateAssetMenu(fileName = "SaveSettings", menuName = "Upscale SDK/Saves/Settings", order = 0)]
    public class SavesSettings : ScriptableObject
    {
        /// <summary>
        /// If true, autosaves will be enabled.2
        /// </summary>
        public bool EnableAutosaves = true;
        /// <summary>
        /// Number of seconds between autosaves.
        /// </summary>
        public int SecondsBetweenAutosaves = 300;
        /// <summary>
        /// File extension used for save files.
        /// </summary>
        public string SaveFileExtension = "txt";
        /// <summary>
        /// Name of the folder that will be mounted during the save. Name should not contain special characters.
        /// </summary>
        public string ProjectFolderName = "UpscaleSaves";
        /// <summary>
        /// If true, the save system will be initialized during the Awake phase of the game.
        /// </summary>
        public bool InitializeSystemOnAwake = true;
        /// <summary>
        /// Save size in MB (approximate). Used on PS4/PS5 to allocate save buffer.
        /// </summary>
        [Space(3f)]
        [Header("PlayStation")]
        public UInt64 SaveSize = 40;
        /// <summary>
        /// Name of the Xbox save container.
        /// Valid characters for the path portion of the container name (up to and including the final forward slash)
        /// includes uppercase letters (A-Z), lowercase letters (a-z), numbers (0-9), underscore (_), and forward slash (/).
        /// The path portion may be empty.
        /// Valid characters for the file name portion (everything after the final forward slash)
        /// include uppercase letters (A-Z), lowercase letters (a-z), numbers (0-9), underscore (_), period (.), and hyphen (-).
        /// The file name may not be empty, end in a period or contain two consecutive periods.
        /// The maximum length for the container name is 256 characters.
        /// </summary>
        [Space(3f)]
        [Header("Xbox")]
        public string XboxContainerName = "UpscaleContainer";
    }
}
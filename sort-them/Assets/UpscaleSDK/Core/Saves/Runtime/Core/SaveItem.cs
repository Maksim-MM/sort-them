using System;

namespace UpscaleSDK.Core.Saves
{
    /// <summary>
    ///  Represents a save item struct.
    /// </summary>
    public struct SaveItem
    {
        /// <summary>
        /// The save's display name (file name without extension).
        /// </summary>
        public string Name;
        /// <summary>
        /// True if this save was produced automatically by the auto-save
        /// timer rather than by an explicit user action (currently unused).
        /// </summary>
        public bool IsAutoSave;
        /// <summary>
        /// When the save was created.
        /// </summary>
        public DateTime Timestamp;
    }
}
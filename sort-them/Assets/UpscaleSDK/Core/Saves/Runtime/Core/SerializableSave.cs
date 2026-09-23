using System;
using System.Collections.Generic;

namespace UpscaleSDK.Core.Saves
{
    /// <summary>
    /// Represents a serializable save struct.
    /// </summary>
    [Serializable]
    public struct SerializableSave
    {
        /// <summary>
        /// The name field.
        /// </summary>
        public string Name;
        /// <summary>
        /// The is auto save field.
        /// </summary>
        public bool IsAutoSave;
        /// <summary>
        /// The timestamp field.
        /// </summary>
        public DateTime Timestamp;
        /// <summary>
        /// The data field.
        /// </summary>
        public Dictionary<string, string> Data;
    }
}
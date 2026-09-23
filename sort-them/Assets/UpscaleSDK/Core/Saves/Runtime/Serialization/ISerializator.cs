namespace UpscaleSDK.Core.Saves.Serialization
{
    /// <summary>
    /// Defines the contract for the serializator.
    /// </summary>
    public interface ISerializator
    {
        /// <summary>
        /// Serializes an object to a string.
        /// </summary>
        /// <param name="obj"> The object to serialize.</param>
        /// <typeparam name="T"> The type of the object to serialize.</typeparam>
        /// <returns> The serialized string representation of the object.</returns>
        public string Serialize<T>(T obj);
        
        /// <summary>
        /// Deserializes a string to an object.
        /// </summary>
        /// <param name="data"> The string to deserialize.</param>
        /// <typeparam name="T"> The type of the object to deserialize to.</typeparam>
        /// <returns> The deserialized object.</returns>
        public T Deserialize<T>(string data);
    }
}
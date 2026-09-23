using Newtonsoft.Json;

namespace UpscaleSDK.Core.Saves.Serialization
{
    /// <summary>
    /// JSON implementation of the ISerializator interface. Uses Newtonsoft.Json for serialization and deserialization.
    /// </summary>
    public class JsonSerializator : ISerializator
    {
        public JsonSerializator(JsonSerializerSettings settings)
        {
            JsonConvert.DefaultSettings = () => settings;
        }

        /// <summary>
        /// Serialize.
        /// </summary>
        /// <param name="obj">The obj.</param>
        public string Serialize<T>(T obj)
        {
            var data = JsonConvert.SerializeObject(obj);
            return data;
        }

        /// <summary>
        /// Deserialize.
        /// </summary>
        /// <param name="data">The data.</param>
        public T Deserialize<T>(string data)
        {
            var obj = JsonConvert.DeserializeObject<T>(data);
            return obj;
        }
    }
}
using Newtonsoft.Json;
using UpscaleSDK.Core.Saves.Serialization;

namespace UpscaleSDK.Core.Saves
{
    /// <summary>
    /// Represents a serializator factory class.
    /// </summary>
    public class SerializatorFactory : IFactory<ISerializator>
    {
        /// <summary>
        /// Create.
        /// </summary>
        public ISerializator Create()
        {
            return new JsonSerializator(new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.All,
                Formatting = Formatting.Indented
            });
        }
    }
}
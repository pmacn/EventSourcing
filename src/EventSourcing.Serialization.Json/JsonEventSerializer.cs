using EventSourcing.Serialization.Exceptions;
using Newtonsoft.Json;
using System;

namespace EventSourcing.Serialization.Json
{
    public class JsonEventSerializer : IEventSerializer
    {
        private readonly JsonSerializerSettings _serializerSettings;

        public JsonEventSerializer()
            : this(new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Objects })
        { }

        public JsonEventSerializer(JsonSerializerSettings settings)
        {
            _serializerSettings = settings;
        }

        public byte[] Serialize(IEvent eventToSerialize)
        {
            try
            {
                var json = JsonConvert.SerializeObject(eventToSerialize, _serializerSettings);
                return GetBytes(json);
            }
            catch (JsonSerializationException ex)
            {
                throw new EventSerializationException("Error serializing event, see inner exception.", ex);
            }
        }

        public IEvent Deserialize(byte[] serializedEvent)
        {
            var json = GetString(serializedEvent);
            try
            {
                var deserialized = JsonConvert.DeserializeObject(json, _serializerSettings);
                return deserialized as IEvent;
            }
            catch (JsonSerializationException ex)
            {
                throw new EventSerializationException("Error deserializing event, see inner exception", ex);
            }
        }

        static byte[] GetBytes(string str)
        {
            var bytes = new byte[str.Length * sizeof(char)];
            Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        static string GetString(byte[] bytes)
        {
            var chars = new char[bytes.Length / sizeof(char)];
            Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
            return new string(chars);
        }
    }
}

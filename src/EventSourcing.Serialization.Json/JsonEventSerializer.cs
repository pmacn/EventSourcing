using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventSourcing.Serialization.Json
{
    public class JsonEventSerializer : IEventSerializer
    {
        public byte[] Serialize(IEvent eventToSerialize)
        {
            var sb = new StringBuilder();
            var serializer = new JsonSerializer();
            try
            {
                serializer.Serialize(new JsonTextWriter(new StringWriter(sb)), eventToSerialize);
            }
            catch (JsonSerializationException ex)
            {
                throw new EventSerializationException("Error serializing event, see inner exception.", ex);
            }

            return Convert.FromBase64String(sb.ToString());
        }

        public IEvent Deserialize(byte[] serializedEvent)
        {
            var json = Convert.ToBase64String(serializedEvent);
            var reader = new JsonTextReader(new StringReader(json));
            try
            {
                return new JsonSerializer().Deserialize(reader) as IEvent;
            }
            catch (JsonSerializationException ex)
            {
                throw new EventSerializationException("Error deserializing event, see inner exception", ex);
            }
        }
    }
}

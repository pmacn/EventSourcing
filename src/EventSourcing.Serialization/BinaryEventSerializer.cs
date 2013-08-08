using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using EventSourcing.Serialization.Exceptions;

namespace EventSourcing.Serialization
{
    public class BinaryEventSerializer : IEventSerializer
    {
        public byte[] Serialize(IEvent eventToSerialize)
        {
            try
            {
                return SerializeImpl(eventToSerialize);
            }
            catch (SerializationException ex)
            {
                throw new EventSerializationException(String.Format("Unable to serialize event of type [{0}]", eventToSerialize.GetType().FullName), ex);
            }
        }

        private static byte[] SerializeImpl(IEvent eventToSerialize)
        {
            using (var stream = new MemoryStream())
            {
                new BinaryFormatter().Serialize(stream, eventToSerialize);
                return stream.ToArray();
            }
        }

        public IEvent Deserialize(byte[] serializedEvent)
        {
            try
            {
                return DeserializeImpl(serializedEvent);
            }
            catch (SerializationException ex)
            {
                throw new EventSerializationException("Unable to deserialize event", ex);
            }
        }

        private static IEvent DeserializeImpl(byte[] serializedEvent)
        {
            using (var stream = new MemoryStream(serializedEvent))
            {
                return (IEvent)new BinaryFormatter().Deserialize(stream);
            }
        }
    }
}


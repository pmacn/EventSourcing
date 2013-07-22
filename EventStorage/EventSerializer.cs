using EventSourcing;
using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace EventStorage
{
    public interface IEventSerializer
    {
        byte[] Serialize(IEvent eventToSerialize);

        IEvent Deserialize(byte[] serializedEvent);
    }

    public class BinaryEventSerializer : IEventSerializer
    {
        [Pure]
        public byte[] Serialize(IEvent eventToSerialize)
        {
            Contract.Requires<ArgumentNullException>(eventToSerialize != null, "eventToSerialize cannot be null");
            try
            {
                return SerializeImpl(eventToSerialize);
            }
            catch (SerializationException ex)
            {
                throw new EventSerializationException(String.Format("Unable to serialize event of type [{0}]", eventToSerialize.GetType().FullName), ex);
            }
        }

        [Pure]
        private static byte[] SerializeImpl(IEvent eventToSerialize)
        {
            using (var stream = new MemoryStream())
            {
                new BinaryFormatter().Serialize(stream, eventToSerialize);
                return stream.ToArray();
            }
        }

        [Pure]
        public IEvent Deserialize(byte[] serializedEvent)
        {
            Contract.Requires<ArgumentNullException>(serializedEvent != null, "serializedEvent cannot be null");

            try
            {
                return DeserializeImpl(serializedEvent);
            }
            catch (SerializationException ex)
            {
                throw new EventSerializationException("Unable to deserialize event", ex);
            }
        }

        [Pure]
        private static IEvent DeserializeImpl(byte[] serializedEvent)
        {
            using (var stream = new MemoryStream(serializedEvent))
            {
                return (IEvent)new BinaryFormatter().Deserialize(stream);
            }
        }
    }
}
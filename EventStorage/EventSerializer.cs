using EventSourcing;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace EventStorage
{
    public interface IEventSerializer
    {
        byte[] Serialize(IEvent eventToSerialize);

        IEvent Deserialize(byte[] serializedEvent);
    }

    public class BinaryEventSerializer : IEventSerializer
    {
        public byte[] Serialize(IEvent eventToSerialize)
        {
            Contract.Requires<ArgumentNullException>(eventToSerialize != null, "eventToSerialize cannot be null");
            try
            {
                return SerializeImpl(eventToSerialize);
            }
            catch (SerializationException ex)
            {
                throw new EventSerializationException("Unable to serialize event", ex);
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

        private IEvent DeserializeImpl(byte[] serializedEvent)
        {
            using (var stream = new MemoryStream(serializedEvent))
            {
                return (IEvent)new BinaryFormatter().Deserialize(stream);
            }
        }
    }
}
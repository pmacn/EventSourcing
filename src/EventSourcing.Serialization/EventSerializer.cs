using System;
using System.Diagnostics.Contracts;

namespace EventSourcing.Serialization
{
    [ContractClass(typeof(EventSerializerContract))]
    public interface IEventSerializer
    {
        byte[] Serialize(IEvent eventToSerialize);

        IEvent Deserialize(byte[] serializedEvent);
    }

    [ContractClassFor(typeof(IEventSerializer))]
    internal abstract class EventSerializerContract : IEventSerializer
    {
        [Pure]
        public byte[] Serialize(IEvent eventToSerialize)
        {
            Contract.Requires<ArgumentNullException>(eventToSerialize != null, "eventToSerialize cannot be null");
            Contract.Ensures(Contract.Result<byte[]>() != null, "Serialize cannot return null");
            throw new NotImplementedException();
        }

        [Pure]
        public IEvent Deserialize(byte[] serializedEvent)
        {
            Contract.Requires<ArgumentNullException>(serializedEvent != null, "serializedEvent cannot be null");
            Contract.Ensures(Contract.Result<IEvent>() != null, "Deserialize cannot return null");
            throw new NotImplementedException();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace EventSourcing
{
    /// <summary>
    /// Should really just be using this as a wrapper around geteventstore.com
    /// But have a few simple implementations anyway, just for fun.
    /// </summary>
    [ContractClass(typeof(EventStoreContract))]
    public interface IEventStore
    {
        EventStream GetEventStreamFor(IIdentity aggregateId, int version = Int32.MaxValue);

        void AppendEventsToStream(IIdentity aggregateId, int expectedVersion, IEvent[] eventsToAppend);
    }

    public class EventStream
    {
        public int StreamVersion;
        public IEnumerable<IEvent> Events;
    }

    [ContractClassFor(typeof(IEventStore))]
    internal abstract class EventStoreContract : IEventStore
    {
        public EventStream GetEventStreamFor(IIdentity aggregateId, int version)
        {
            Contract.Requires<ArgumentNullException>(aggregateId != null, "aggregateId cannot be null");
            Contract.Requires<ArgumentOutOfRangeException>(version >= 0, "version cannot be negative");
            throw new NotImplementedException();
        }

        public void AppendEventsToStream(IIdentity aggregateId, int expectedVersion, IEvent[] eventsToAppend)
        {
            Contract.Requires<ArgumentNullException>(aggregateId != null, "aggregateId cannot be null");
            Contract.Requires<ArgumentOutOfRangeException>(expectedVersion >= 0, "expectedVersion cannot be negative");
            Contract.Requires<ArgumentNullException>(eventsToAppend != null, "eventsToAppend cannot be null");
            Contract.Requires<ArgumentNullException>(Contract.ForAll(eventsToAppend, e => e != null), "None of the events in eventsToAppend can be null");
            throw new NotImplementedException();
        }
    }
}
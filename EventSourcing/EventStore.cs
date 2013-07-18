using System;
using System.Collections.Generic;

namespace ELI.EventSourcing
{
    /// <summary>
    /// Should really just be using this as a wrapper around geteventstore.com
    /// But have a few original implementations anyway, just in case.
    /// </summary>
    public interface IEventStore
    {
        EventStream GetEventStreamFor(IIdentity aggregateId);
        void AppendEventsToStream(IIdentity aggregateId, long expectedVersion, IEnumerable<IEvent> eventsToAppend);
    }

    public class EventStream
    {
        public long StreamVersion;
        public IEnumerable<IEvent> Events;
    }

    public class Repository<TAggregate> where TAggregate : IAggregateRoot
    {
        private readonly IEventStore _store;

        public Repository(IEventStore store)
        {
            _store = store;
        }

        public TAggregate GetById(IIdentity aggregateId)
        {
            var stream = _store.GetEventStreamFor(aggregateId);
            var ctor = typeof(TAggregate).GetConstructor(new [] { typeof(IEnumerable<IEvent>) });
            if(ctor == null)
                throw new AggregateConstructionException();

            return (TAggregate)ctor.Invoke(new [] { stream.Events });
        }
    }

    public class AggregateConstructionException : System.Exception
    {
    }
}
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace EventSourcing
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

    public class Repository
    {
        private readonly IEventStore _store;

        public Repository(IEventStore store)
        {
            Contract.Requires(store != null, "store cannot be null");

            _store = store;
        }

        public TAggregate GetById<TAggregate>(IIdentity aggregateId)
            where TAggregate : IAggregateRoot
        {
            Contract.Requires(aggregateId != null, "aggregateId cannot be null");

            var stream = _store.GetEventStreamFor(aggregateId);
            var ctor = typeof(TAggregate).GetConstructor(new [] { typeof(IEnumerable<IEvent>) });
            if(ctor == null)
                throw new AggregateConstructionException(String.Format("Unable to find constructor that takes a history of events for type {0}", typeof(TAggregate).Name);

            return (TAggregate)ctor.Invoke(new [] { stream.Events });
        }

        public void Save<TIdentity>(IAggregateRoot<TIdentity> aggregate, long expectedVersion)
            where TIdentity : IIdentity
        {
            Contract.Requires<ArgumentNullException>(aggregate != null, "aggregate cannot be null");
            Contract.Requires<ArgumentOutOfRangeException>(expectedVersion >= 0, "expectedVersion cannot be negative");

            _store.AppendEventsToStream(aggregate.Id, expectedVersion, aggregate.UncommittedEvents);
        }
    }

    [Serializable]
    public class AggregateConstructionException : Exception
    {
        public AggregateConstructionException() { }
        public AggregateConstructionException(string message) : base(message) { }
        public AggregateConstructionException(string message, Exception inner) : base(message, inner) { }
        protected AggregateConstructionException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}
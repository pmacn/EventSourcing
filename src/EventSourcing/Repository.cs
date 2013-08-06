using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace EventSourcing
{
    [ContractClass(typeof(IRepositoryContract))]
    public interface IRepository
    {
        TAggregate GetById<TAggregate>(IIdentity aggregateId)
            where TAggregate : IAggregateRoot;

        void Save<TIdentity>(IAggregateRoot<TIdentity> aggregate)
            where TIdentity : IIdentity;
    }

    [ContractClassFor(typeof(IRepository))]
    internal abstract class IRepositoryContract : IRepository
    {
        [Pure]
        public TAggregate GetById<TAggregate>(IIdentity aggregateId) where TAggregate : IAggregateRoot
        {
            Contract.Requires<ArgumentNullException>(aggregateId != null, "aggregateId cannot be null");
            Contract.Ensures(Contract.Result<TAggregate>() != null, "GetById cannot return null");
            throw new NotImplementedException();
        }

        public void Save<TIdentity>(IAggregateRoot<TIdentity> aggregate) where TIdentity : IIdentity
        {
            Contract.Requires<ArgumentNullException>(aggregate != null, "aggregate cannot be null");
            Contract.Requires<ArgumentException>(aggregate.UncommittedEvents != null, "aggregate.UncommittedEvents cannot be null");
            throw new NotImplementedException();
        }
    }

    public class Repository : IRepository
    {
        private readonly IEventStore _store;

        public Repository(IEventStore store)
        {
            Contract.Requires<ArgumentNullException>(store != null, "store cannot be null");
            _store = store;
        }

        public TAggregate GetById<TAggregate>(IIdentity aggregateId)
            where TAggregate : IAggregateRoot
        {
            var stream = _store.GetEventStreamFor(aggregateId);
            var ctor = typeof(TAggregate).GetConstructor(Type.EmptyTypes);
            if (ctor == null)
                throw new AggregateConstructionException(String.Format("Unable to find constructor that takes a history of events for type {0}", typeof(TAggregate).Name));

            var agg = (TAggregate)ctor.Invoke(Type.EmptyTypes);
            agg.LoadFrom(stream.Events);
            return agg;
        }

        public void Save<TIdentity>(IAggregateRoot<TIdentity> aggregate)
            where TIdentity : IIdentity
        {
            var expectedVersion = aggregate.Version - aggregate.UncommittedEvents.Count();
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
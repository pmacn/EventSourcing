using EventSourcing.Exceptions;
using System;
using System.Diagnostics.Contracts;
using System.Linq;

namespace EventSourcing
{
    [ContractClass(typeof(RepositoryContract))]
    public interface IRepository
    {
        TAggregate GetById<TAggregate>(IAggregateIdentity aggregateId)
            where TAggregate : IAggregateRoot;

        void Save<TIdentity>(IAggregateRoot<TIdentity> aggregate)
            where TIdentity : IAggregateIdentity;
    }

    public class Repository : IRepository
    {
        private readonly IEventStore _store;

        public Repository(IEventStore store)
        {
            Contract.Requires<ArgumentNullException>(store != null, "store cannot be null");
            _store = store;
        }

        public TAggregate GetById<TAggregate>(IAggregateIdentity aggregateId)
            where TAggregate : IAggregateRoot
        {
            var stream = _store.GetEventStreamFor(aggregateId);
            var ctor = typeof(TAggregate).GetConstructor(Type.EmptyTypes);
            if (ctor == null)
                throw new AggregateConstructionException(String.Format("Unable to find constructor that takes a history of events for type {0}", typeof(TAggregate).Name));

            var agg = (TAggregate)ctor.Invoke(null);
            agg.LoadFrom(stream.Events);
            return agg;
        }

        public void Save<TIdentity>(IAggregateRoot<TIdentity> aggregate)
            where TIdentity : IAggregateIdentity
        {
            var expectedVersion = aggregate.Version - aggregate.UncommittedEvents.Count();
            _store.AppendEventsToStream(aggregate.Id, expectedVersion, aggregate.UncommittedEvents.ToArray());
        }
    }

    [ContractClassFor(typeof(IRepository))]
    internal abstract class RepositoryContract : IRepository
    {
        [Pure]
        public TAggregate GetById<TAggregate>(IAggregateIdentity aggregateId) where TAggregate : IAggregateRoot
        {
            Contract.Requires<ArgumentNullException>(aggregateId != null, "aggregateId cannot be null");
            Contract.Ensures(Contract.Result<TAggregate>() != null, "GetById cannot return null");
            throw new NotImplementedException();
        }

        public void Save<TIdentity>(IAggregateRoot<TIdentity> aggregate) where TIdentity : IAggregateIdentity
        {
            Contract.Requires<ArgumentNullException>(aggregate != null, "aggregate cannot be null");
            Contract.Requires<ArgumentException>(aggregate.UncommittedEvents != null, "aggregate.UncommittedEvents cannot be null");
            throw new NotImplementedException();
        }
    }
}
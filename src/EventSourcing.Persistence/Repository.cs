using EventSourcing.Exceptions;
using System;
using System.Diagnostics.Contracts;
using System.Linq;

namespace EventSourcing.Persistence
{
    [ContractClass(typeof(RepositoryContract))]
    public interface IRepository
    {
        TAggregate GetById<TAggregate>(IAggregateIdentity aggregateId)
            where TAggregate : class, IAggregateRoot;

        void Save(IAggregateRoot aggregate);
    }

    public class Repository : IRepository
    {
        private readonly IEventStore _store;

        private readonly IAggregateFactory _aggregateFactory;

        public Repository(IEventStore store, IAggregateFactory aggregateFactory)
        {
            Contract.Requires<ArgumentNullException>(store != null, "store cannot be null");
            _aggregateFactory = aggregateFactory;
            _store = store;
        }

        public TAggregate GetById<TAggregate>(IAggregateIdentity aggregateId)
            where TAggregate : class, IAggregateRoot
        {
            var stream = _store.GetEventStreamFor(aggregateId);
            var aggregate = _aggregateFactory.Create<TAggregate>();
            aggregate.LoadFrom(stream.Events);
            return aggregate;
        }

        public void Save(IAggregateRoot aggregate)
        {
            var expectedVersion = aggregate.Version - aggregate.UncommittedEvents.Count();
            _store.AppendEventsToStream(aggregate.Id, expectedVersion, aggregate.UncommittedEvents.ToArray());
        }
    }

    [ContractClassFor(typeof(IRepository))]
    internal abstract class RepositoryContract : IRepository
    {
        [Pure]
        public TAggregate GetById<TAggregate>(IAggregateIdentity aggregateId)
            where TAggregate : class, IAggregateRoot
        {
            Contract.Requires<ArgumentNullException>(aggregateId != null, "aggregateId cannot be null");
            Contract.Ensures(Contract.Result<TAggregate>() != null, "GetById cannot return null");
            throw new NotImplementedException();
        }

        public void Save(IAggregateRoot aggregate)
        {
            Contract.Requires<ArgumentNullException>(aggregate != null, "aggregate cannot be null");
            Contract.Requires<ArgumentException>(aggregate.UncommittedEvents != null, "aggregate.UncommittedEvents cannot be null");
            throw new NotImplementedException();
        }
    }
}
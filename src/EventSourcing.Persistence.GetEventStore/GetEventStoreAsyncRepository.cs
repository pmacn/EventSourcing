using EventSourcing.Exceptions;
using EventSourcing.Persistence.Exceptions;
using EventSourcing.Serialization;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EventSourcing.Persistence.GetEventStore
{

    public class GetEventStoreAsyncRepository : IAsyncRepository
    {
        private const int SliceSize = 500;

        private readonly IEventStoreConnection _connection;

        private readonly IEventSerializer _eventSerializer;

        private readonly IAggregateFactory _aggregateFactory;

        public GetEventStoreAsyncRepository(IEventStoreConnection connection, IEventSerializer eventSerializer, IAggregateFactory aggregateFactory)
        {
            _connection = connection;
            _eventSerializer = eventSerializer;
            _aggregateFactory = aggregateFactory;
        }

        public async Task<TAggregate> GetByIdAsync<TAggregate>(IAggregateIdentity aggregateId)
            where TAggregate : class, IAggregateRoot
        {
            var aggregate = _aggregateFactory.Create<TAggregate>();
            var history = await GetEventsFor(aggregateId);
            aggregate.LoadFrom(history);
            return aggregate;
        }

        private async Task<IEvent[]> GetEventsFor(IAggregateIdentity aggregateId)
        {
            var sliceStart = 1;
            var resolvedEVents = new List<ResolvedEvent>();
            StreamEventsSlice slice;

            do
            {
                slice = await _connection.ReadStreamEventsForwardAsync(aggregateId.ToString(),
                                                                           sliceStart, SliceSize, false);
                if (slice.Status == SliceReadStatus.StreamNotFound)
                    return new IEvent[0];
                if (slice.Status == SliceReadStatus.StreamDeleted)
                    throw new AggregateDeletedException();

                resolvedEVents.AddRange(slice.Events);
                sliceStart = slice.NextEventNumber;
            } while (!slice.IsEndOfStream);

            return resolvedEVents.Select(GetEvent).ToArray();
        }

        private IEvent GetEvent(ResolvedEvent resolvedEvent)
        {
            var eventData = resolvedEvent.OriginalEvent.Data;
            return _eventSerializer.Deserialize(eventData);
        }

        public async Task SaveAsync<TIdentity>(IAggregateRoot<TIdentity> aggregate) where TIdentity : class, IAggregateIdentity
        {
            var expectedVersion = aggregate.Version - aggregate.UncommittedEvents.Count();
            var eventData = aggregate.UncommittedEvents.Select(CreateEventData);
            try
            {
                await _connection.AppendToStreamAsync(aggregate.Id.ToString(), expectedVersion, eventData);
            }
            catch (WrongExpectedVersionException ex)
            {
                // TODO: Deal with conflict resolution
                throw new AggregateConcurrencyException("", ex);
            }
        }

        private EventData CreateEventData(IEvent arg)
        {
            var data = _eventSerializer.Serialize(arg);
            return new EventData(Guid.NewGuid(), arg.GetType().Name, false, data, null); // TODO: Deal with metadata
        }
    }
}

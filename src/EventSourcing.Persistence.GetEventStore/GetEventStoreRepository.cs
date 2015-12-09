using EventSourcing.Exceptions;
using EventSourcing.Persistence.Exceptions;
using EventSourcing.Serialization;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EventSourcing.Persistence.GetEventStore
{
    public class GetEventStoreRepository : IRepository
    {
        private readonly IEventStoreConnection _connection;

        private readonly IEventSerializer _serializer;

        private readonly IConflictDetector _conflictDetector;

        private readonly IAggregateFactory _aggregateFactory;

        private const int SliceSize = 500;

        public GetEventStoreRepository(
            IEventStoreConnection connection,
            IEventSerializer serializer,
            IConflictDetector conflictDetector,
            IAggregateFactory aggregateFactory)
        {
            _connection = connection;
            _serializer = serializer;
            _conflictDetector = conflictDetector;
            _aggregateFactory = aggregateFactory;
        }

        public TAggregate GetById<TAggregate>(IAggregateIdentity aggregateId)
            where TAggregate : class, IAggregateRoot
        {
            var aggregate = _aggregateFactory.Create<TAggregate>();
            var events = GetEventsFor(aggregateId);
            aggregate.LoadFrom(events);
            return aggregate;
        }

        private IEvent[] GetEventsFor(IAggregateIdentity aggregateId, int fromVersion = 1)
        {
            var streamName = GetStreamName(aggregateId);
            var events = new List<ResolvedEvent>();
            StreamEventsSlice slice;
            int streamPosition = fromVersion;
            do
            {
                slice = _connection.ReadStreamEventsForward(streamName, streamPosition, SliceSize, false);
                if(slice.Status == SliceReadStatus.StreamNotFound)
                    return new IEvent[0];
                if (slice.Status == SliceReadStatus.StreamDeleted)
                    throw new AggregateDeletedException();

                streamPosition = slice.NextEventNumber;
                events.AddRange(slice.Events);
            }
            while (!slice.IsEndOfStream);
            
            return events.Select(GetEvent).ToArray();
        }

        public void Save(IAggregateRoot aggregate)
        {
            var streamName = GetStreamName(aggregate.Id);
            var expectedVersion = aggregate.Version - aggregate.UncommittedEvents.Count();
            var eventData = aggregate.UncommittedEvents.Select(CreateEventData).ToArray();
            while (true)
            {
                try
                {
                    _connection.AppendToStream(streamName, expectedVersion, eventData);
                    aggregate.UncommittedEvents.MarkAsCommitted();
                    return;
                }
                catch (WrongExpectedVersionException ex)
                {
                    var committedEvents = GetEventsFor(aggregate.Id, expectedVersion);
                    if (_conflictDetector.HasConflict(committedEvents, aggregate.UncommittedEvents))
                        throw new AggregateConcurrencyException("", ex); // TODO: Need a message

                    expectedVersion += committedEvents.Count();
                }
            }
        }

        private EventData CreateEventData(IEvent @event)
        {
            var serializedEvent = _serializer.Serialize(@event);
            var eventTypeName = @event.GetType().Name;
            return new EventData(Guid.NewGuid(), eventTypeName, false, serializedEvent, null);
        }

        private IEvent GetEvent(ResolvedEvent resolvedEvent)
        {
            return _serializer.Deserialize(resolvedEvent.OriginalEvent.Data);
        }

        private static string GetStreamName(IAggregateIdentity id)
        {
            return id.ToString();
        }
    }
}

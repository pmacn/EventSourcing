using EventSourcing;
using EventStore;
using EventStore.ClientAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net;

namespace EventStorage
{
    public class MyEventStore : IEventStore
    {
        private readonly IEventPersistance _persistance;

        private readonly IEventPublisher _publisher;

        private readonly IConflictDetector _conflictDetector;

        public MyEventStore(IEventPersistance persistance, IEventPublisher publisher, IConflictDetector conflictDetector)
        {
            Contract.Requires<ArgumentNullException>(persistance != null, "persistance cannot be null");
            Contract.Requires<ArgumentNullException>(publisher != null, "publisher cannot be null");

            _persistance = persistance;
            _publisher = publisher;
            _conflictDetector = conflictDetector;
        }

        public EventStream GetEventStreamFor(IIdentity aggregateId)
        {
            var events = _persistance.GetEventsFor(aggregateId).ToList();
            return new EventStream { StreamVersion = events.Count(), Events = events };
        }

        public void AppendEventsToStream(IIdentity aggregateId, int expectedVersion, IEnumerable<IEvent> eventsToAppend)
        {
            var events = eventsToAppend.ToArray();
            if(!events.Any())
                return;

            var actualVersion = _persistance.GetVersionFor(aggregateId);
            if (actualVersion != expectedVersion)
            {
                var committedEvents = _persistance.GetEventsFor(aggregateId).Skip(expectedVersion).ToList();
                if (_conflictDetector.HasConflict(committedEvents, eventsToAppend))
                    throw new AggregateConcurrencyException(expectedVersion, actualVersion);

                expectedVersion += committedEvents.Count;
            }

            _persistance.AppendEvents(aggregateId, events);
            _publisher.Publish(events);
        }
    }

    public class OtherEventStore : IEventStore
    {
        private const int ReadSliceSize = 500;

        private readonly IEventStoreConnection _connection;

        private readonly IEventSerializer _serializer;

        private static readonly Func<IIdentity, string> StreamNameFactory = id => id.ToString();

        public OtherEventStore(string ipAddress, int port, IEventSerializer serializer)
        {
            var endPoint = new IPEndPoint(IPAddress.Parse(ipAddress), port);
            _connection = EventStoreConnection.Create(endPoint, "myConnection");
            _serializer = serializer;
        }

        public EventStream GetEventStreamFor(IIdentity aggregateId)
        {
            var events = new List<IEvent>();
            const int version = int.MaxValue;
            var streamName = StreamNameFactory(aggregateId);
            var sliceStart = 1;
            var numberOfEventsToRead = Math.Min(ReadSliceSize, version - sliceStart + 1);
            StreamEventsSlice currentSlice;
            do
            {
                currentSlice = _connection.ReadStreamEventsForward(streamName, sliceStart, numberOfEventsToRead, false);
                if (currentSlice.Status == SliceReadStatus.StreamNotFound)
                    return new EventStream { Events = new List<IEvent>(), StreamVersion = 0 };
                if (currentSlice.Status == SliceReadStatus.StreamDeleted)
                    throw new AggregateDeletedException();

                events.AddRange(currentSlice.Events.Select(EventFromData));

                sliceStart = currentSlice.NextEventNumber;
            } while (version >= currentSlice.NextEventNumber && !currentSlice.IsEndOfStream);

            return new EventStream { Events = events, StreamVersion = events.Count };
        }

        public void AppendEventsToStream(IIdentity aggregateId, int expectedVersion, IEnumerable<IEvent> eventsToAppend)
        {
            var streamName = StreamNameFactory(aggregateId);
            _connection.AppendToStream(streamName, expectedVersion, eventsToAppend.Select(CreateEventData));
        }

        private EventData CreateEventData(IEvent e)
        {
            return new EventData(Guid.NewGuid(), e.GetType().Name, false, _serializer.Serialize(e), null);
        }

        private IEvent EventFromData(ResolvedEvent resolvedEvent)
        {
            return _serializer.Deserialize(resolvedEvent.OriginalEvent.Data);
        }
    }

    public class AggregateDeletedException : Exception
    {
    }

    public interface IConflictDetector
    {
        bool HasConflict(IEnumerable<IEvent> committed, IEnumerable<IEvent> uncommitted);
    }

    public class DelegateConflictDetector : IConflictDetector
    {
        private Dictionary<Type, Dictionary<Type, Func<IEvent, IEvent, bool>>> _delegates = new Dictionary<Type, Dictionary<Type, Func<IEvent, IEvent, bool>>>();

        public bool HasConflict(IEnumerable<IEvent> committedEvents, IEnumerable<IEvent> uncommittedEvents)
        {
            var conflictingEvents = new List<IEvent>();
            foreach (var committed in committedEvents)
                foreach (var uncommitted in uncommittedEvents)
                    if (Conflicts(committed, uncommitted))
                        return true;

            return false;
        }

        private bool Conflicts(IEvent committed, IEvent uncommitted)
        {
            Dictionary<Type, Func<IEvent, IEvent, bool>> _delegatesForCommittedType;
            if (!_delegates.TryGetValue(committed.GetType(), out _delegatesForCommittedType))
                return true;

            Func<IEvent, IEvent, bool> conflictDelegate;
            if (_delegatesForCommittedType.TryGetValue(uncommitted.GetType(), out conflictDelegate))
                return true;

            return conflictDelegate(committed, uncommitted);
        }
    }
}

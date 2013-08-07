using System.Runtime.Serialization;
using EventSourcing;
using EventStore;
using EventStore.ClientAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net;
using System.Collections.Concurrent;
using EventStore.ClientAPI.Exceptions;

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

        public EventStream GetEventStreamFor(IIdentity aggregateId, int version)
        {
            var events = _persistance.GetEventsFor(aggregateId).Take(version).ToList();
            return new EventStream { StreamVersion = events.Count(), Events = events };
        }

        public void AppendEventsToStream(IIdentity aggregateId, int expectedVersion, IEvent[] eventsToAppend)
        {
            var events = eventsToAppend as IEvent[] ?? eventsToAppend.ToArray();
            if(!events.Any())
                return;

            var actualVersion = _persistance.GetVersionFor(aggregateId);
            if (actualVersion != expectedVersion)
            {
                var committedEvents = _persistance.GetEventsFor(aggregateId).Skip(expectedVersion).ToList();
                if (_conflictDetector.HasConflict(committedEvents, events))
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
        private readonly IConflictDetector _conflictDetector;

        private static readonly Func<IIdentity, string> StreamNameFactory = id => id.ToString();

        private readonly ConcurrentDictionary<string, WeakReference<List<IEvent>>> _cache =
            new ConcurrentDictionary<string, WeakReference<List<IEvent>>>();

        public OtherEventStore(string ipAddress, int port, IEventSerializer serializer, IConflictDetector conflictDetector)
        {
            var endPoint = new IPEndPoint(IPAddress.Parse(ipAddress), port);
            _connection = EventStoreConnection.Create(endPoint, "myConnection");
            _serializer = serializer;
            _conflictDetector = conflictDetector;
        }

        public EventStream GetEventStreamFor(IIdentity aggregateId, int version)
        {
            var streamName = StreamNameFactory(aggregateId);
            if (_cache.ContainsKey(streamName))
            {
                List<IEvent> meh;
                if (_cache[streamName].TryGetTarget(out meh))
                    return new EventStream { Events = meh, StreamVersion = meh.Count };
            }
            var events = new List<IEvent>();
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

            var cacheReference = new WeakReference<List<IEvent>> (events);
            _cache.AddOrUpdate(streamName, cacheReference, (s, r) => cacheReference);
            return new EventStream { Events = events, StreamVersion = events.Count };
        }

        public void AppendEventsToStream(IIdentity aggregateId, int expectedVersion, IEvent[] eventsToAppend)
        {
            var streamName = StreamNameFactory(aggregateId);
            var eventData = eventsToAppend.Select(CreateEventData);
            try
            {
                _connection.AppendToStream(streamName, expectedVersion, eventData);
            }
            catch (WrongExpectedVersionException ex)
            {
                var committedEvents = _connection.ReadStreamEventsForward(streamName, expectedVersion, Int32.MaxValue, false).Events.Select(EventFromData);
                if (_conflictDetector.HasConflict(committedEvents, eventsToAppend))
                    throw new AggregateConcurrencyException();
            }
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

    [Serializable]
    public class AggregateDeletedException : Exception
    {
        public AggregateDeletedException() { }
        public AggregateDeletedException(string message) : base(message) { }
        public AggregateDeletedException(string message, Exception inner)
            : base(message, inner) { }
        protected AggregateDeletedException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        { }
    }
}

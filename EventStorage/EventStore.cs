using EventSourcing;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using EventStore.ClientAPI;
using System.Net;

namespace EventStorage
{
    public class MyEventStore : IEventStore
    {
        private readonly IEventPersistance _persistance;
        private readonly IEventPublisher _publisher;

        public MyEventStore(IEventPersistance persistance, IEventPublisher publisher)
        {
            Contract.Requires<ArgumentNullException>(persistance != null, "persistance cannot be null");
            Contract.Requires<ArgumentNullException>(publisher != null, "publisher cannot be null");

            _persistance = persistance;
            _publisher = publisher;
        }

        public EventStream GetEventStreamFor(IIdentity aggregateId)
        {
            Contract.Requires<ArgumentNullException>(aggregateId != null, "aggregateId cannot be null");

            var events = _persistance.GetEventsFor(aggregateId);
            return new EventStream { StreamVersion = events.LongCount(), Events = events };
        }

        public void AppendEventsToStream(IIdentity aggregateId, long expectedVersion, IEnumerable<IEvent> eventsToAppend)
        {
            Contract.Requires<ArgumentNullException>(aggregateId != null, "aggregateId cannot be null");
            Contract.Requires<ArgumentNullException>(eventsToAppend != null, "eventsToAppend cannot be null");
            Contract.Requires(Contract.ForAll(eventsToAppend, e => e != null), "none of the events in eventsToAppend can be null");

            if(!eventsToAppend.Any())
                return;

            var actualVersion = _persistance.GetVersionFor(aggregateId);
            if(actualVersion != expectedVersion)
                throw new AggregateConcurrencyException(expectedVersion, actualVersion);

            _persistance.AppendEvents(aggregateId, eventsToAppend);
            _publisher.Publish(eventsToAppend);
        }
    }

    public class OtherEventStore : IEventStore
    {
        private readonly IEventStoreConnection _connection;
        Func<IIdentity, string> _streamNameFactory;
        IEventSerializer _serializer;

        public OtherEventStore()
        {
            _connection = EventStoreConnection.Create(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8181), "myConnection");
            _streamNameFactory = id => String.Concat(id.GetTag(), id.GetId());
        }

        public EventStream GetEventStreamFor(IIdentity aggregateId)
        {
            var streamName = _streamNameFactory(aggregateId);
            var slice = _connection.ReadStreamEventsForward(streamName, 0, int.MaxValue, false);
            if(slice.Status == SliceReadStatus.StreamNotFound)
                return new EventStream
                {
                    Events = new List<IEvent>(),
                    StreamVersion = 0
                };

            return new EventStream
            {
                Events = slice.Events.Select(e => _serializer.Deserialize(e.OriginalEvent.Data)),
                StreamVersion = slice.LastEventNumber
            };
        }

        public void AppendEventsToStream(IIdentity aggregateId, long expectedVersion, IEnumerable<IEvent> eventsToAppend)
        {
            var streamName = _streamNameFactory(aggregateId);
            _connection.AppendToStream(streamName, (int)expectedVersion, eventsToAppend.Select(CreateEventData));
        }

        private EventData CreateEventData(IEvent arg)
        {
            return new EventData(Guid.NewGuid(), arg.GetType().Name, false, _serializer.Serialize(arg), )
        }
    }
}
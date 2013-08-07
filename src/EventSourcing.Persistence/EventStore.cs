using System.Runtime.Serialization;
using EventSourcing;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net;
using System.Collections.Concurrent;
using EventSourcing.Serialization;
using EventSourcing.Exceptions;
using EventSourcing.Persistence.Exceptions;

namespace EventSourcing.Persistence
{
    /// <summary>
    /// Should really just be using this as a wrapper around geteventstore.com
    /// But have a few simple implementations anyway, just for fun.
    /// </summary>
    [ContractClass(typeof(EventStoreContract))]
    public interface IEventStore
    {
        EventStream GetEventStreamFor(IAggregateIdentity aggregateId, int version = Int32.MaxValue);

        void AppendEventsToStream(IAggregateIdentity aggregateId, int expectedVersion, IEvent[] eventsToAppend);
    }

    public class EventStream
    {
        public int StreamVersion;
        public IEnumerable<IEvent> Events;
    }

    [ContractClassFor(typeof(IEventStore))]
    internal abstract class EventStoreContract : IEventStore
    {
        public EventStream GetEventStreamFor(IAggregateIdentity aggregateId, int version)
        {
            Contract.Requires<ArgumentNullException>(aggregateId != null, "aggregateId cannot be null");
            Contract.Requires<ArgumentOutOfRangeException>(version >= 0, "version cannot be negative");
            throw new NotImplementedException();
        }

        public void AppendEventsToStream(IAggregateIdentity aggregateId, int expectedVersion, IEvent[] eventsToAppend)
        {
            Contract.Requires<ArgumentNullException>(aggregateId != null, "aggregateId cannot be null");
            Contract.Requires<ArgumentOutOfRangeException>(expectedVersion >= 0, "expectedVersion cannot be negative");
            Contract.Requires<ArgumentNullException>(eventsToAppend != null, "eventsToAppend cannot be null");
            Contract.Requires<ArgumentNullException>(Contract.ForAll(eventsToAppend, e => e != null), "None of the events in eventsToAppend can be null");
            throw new NotImplementedException();
        }
    }

    public class EventStore : IEventStore
    {
        private readonly IEventPersistance _persistance;

        private readonly IEventPublisher _publisher;

        private readonly IConflictDetector _conflictDetector;

        public EventStore(IEventPersistance persistance, IEventPublisher publisher, IConflictDetector conflictDetector)
        {
            Contract.Requires<ArgumentNullException>(persistance != null, "persistance cannot be null");
            Contract.Requires<ArgumentNullException>(publisher != null, "publisher cannot be null");

            _persistance = persistance;
            _publisher = publisher;
            _conflictDetector = conflictDetector;
        }

        public EventStream GetEventStreamFor(IAggregateIdentity aggregateId, int version)
        {
            var events = _persistance.GetEventsFor(aggregateId).Take(version).ToList();
            return new EventStream { StreamVersion = events.Count(), Events = events };
        }

        public void AppendEventsToStream(IAggregateIdentity aggregateId, int expectedVersion, IEvent[] eventsToAppend)
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
}

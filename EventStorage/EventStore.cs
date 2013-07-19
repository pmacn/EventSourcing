using EventSourcing;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace EventStorage
{
    public class EventStore : IEventStore
    {
        private readonly IEventPersistance _persistance;

        public EventStore(IEventPersistance persistance)
        {
            _persistance = persistance;
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

            if(!eventsToAppend.Any())
                return;

            var actualVersion = _persistance.GetVersionFor(aggregateId);
            if(actualVersion != expectedVersion)
                throw new AggregateConcurrencyException(expectedVersion, actualVersion);

            _persistance.AppendEvents(aggregateId, eventsToAppend);
        }
    }
}
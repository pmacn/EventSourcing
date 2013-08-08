using EventSourcing.Exceptions;
using EventSourcing.Persistence.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Contracts;
using System.Linq;

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

    public class EventStore : IEventStore
    {
        private readonly IEventPersistance _persistance;

        private readonly IEventPublisher _publisher;

        private readonly IConflictDetector _conflictDetector;

        public EventStore(IEventPersistance persistance, IEventPublisher publisher, IConflictDetector conflictDetector)
        {
            Contract.Requires<ArgumentNullException>(persistance != null, "persistance cannot be null");
            Contract.Requires<ArgumentNullException>(publisher != null, "publisher cannot be null");
            Contract.Requires<ArgumentNullException>(conflictDetector != null, "conflictDetector cannot be null");

            _persistance = persistance;
            _publisher = publisher;
            _conflictDetector = conflictDetector;
        }

        public EventStream GetEventStreamFor(IAggregateIdentity aggregateId, int version)
        {
            var events = _persistance.GetEventsFor(aggregateId, version).ToList();
            return new EventStream { StreamVersion = events.Count(), Events = events };
        }

        public void AppendEventsToStream(IAggregateIdentity aggregateId, int expectedVersion, IEvent[] eventsToAppend)
        {
            if(!eventsToAppend.Any())
                return;

            var actualVersion = _persistance.GetVersionFor(aggregateId);
            if (actualVersion != expectedVersion)
            {
                var committedEvents = _persistance.GetEventsFor(aggregateId).Skip(expectedVersion).ToList();
                if (_conflictDetector.HasConflict(committedEvents, eventsToAppend))
                    throw new AggregateConcurrencyException(expectedVersion, actualVersion);

                AppendEventsToStream(aggregateId, expectedVersion + committedEvents.Count, eventsToAppend);
                return;
            }

            _persistance.AppendEvents(aggregateId, eventsToAppend);
            _publisher.Publish(eventsToAppend);
        }
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

    #region Expanding the stream concept, some other day

    public interface IEventStream : IDisposable
    {
        string Id { get; }
        IEnumerable<IEvent> CommittedEvents { get; }
        IEnumerable<IEvent> UncommittedEvents { get; }
        void Append(IEvent eventToAppend);
        void CommitChanges();
    }

    public sealed class OtherEventStream : IEventStream
    {
        List<IEvent> _committedEvents = new List<IEvent>();

        List<IEvent> _uncommittedEvents = new List<IEvent>();

        bool IsDisposed = false;

        public string Id { get; private set; }

        public IEnumerable<IEvent> CommittedEvents
        {
            get
            {
                ThrowIfDisposed();
                return _committedEvents.ToList().AsEnumerable();
            }
        }

        public IEnumerable<IEvent> UncommittedEvents
        {
            get
            {
                ThrowIfDisposed();
                return _uncommittedEvents.ToList().AsEnumerable();
            }
        }

        public void Append(IEvent eventToAppend)
        {
            ThrowIfDisposed();
            _uncommittedEvents.Add(eventToAppend);
        }

        public void CommitChanges()
        {
            ThrowIfDisposed();
            _committedEvents.AddRange(_uncommittedEvents);
            _uncommittedEvents.Clear();
        }

        public void Dispose()
        {
            if (IsDisposed)
                return;

            IsDisposed = true;
            _uncommittedEvents.Clear();
        }

        private void ThrowIfDisposed()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(PersistenceResources.IsDisposed);
        }
    }

    public interface IOtherEventStore
    {
        IEventStream CreateStream(string streamName);
        IEventStream OpenStream(string streamName, int from, int to);
    } 
    #endregion
}

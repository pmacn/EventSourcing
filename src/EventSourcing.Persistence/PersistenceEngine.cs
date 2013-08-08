using EventSourcing.Serialization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;

namespace EventSourcing.Persistence
{
    [ContractClass(typeof(EventPersistanceContract))]
    public interface IEventPersistance
    {
        void AppendEvents(IAggregateIdentity aggregateId, IEnumerable<IEvent> eventsToAppend);

        IEnumerable<IEvent> GetEventsFor(IAggregateIdentity aggregateId, int version = Int32.MaxValue);

        int GetVersionFor(IAggregateIdentity aggregateId);
    }

    public class MemoryEventPersistance : IEventPersistance
    {
        private readonly ConcurrentDictionary<IAggregateIdentity, List<byte[]>> _events = new ConcurrentDictionary<IAggregateIdentity, List<byte[]>>();

        private readonly IEventSerializer _serializer;

        public MemoryEventPersistance(IEventSerializer serializer)
        {
            _serializer = serializer;
        }

        public int GetVersionFor(IAggregateIdentity aggregateId)
        {
            return EventsFor(aggregateId).Count;
        }

        public IEnumerable<IEvent> GetEventsFor(IAggregateIdentity aggregateId, int version)
        {
            var eventData = EventsFor(aggregateId).Take(version).ToList();
            return eventData.Select(_serializer.Deserialize);
        }

        private List<byte[]> EventsFor(IAggregateIdentity aggregateId)
        {
            return _events.GetOrAdd(aggregateId, new List<byte[]>());
        }

        public void AppendEvents(IAggregateIdentity aggregateId, IEnumerable<IEvent> eventsToAppend)
        {
            var eventData = eventsToAppend.Select(_serializer.Serialize);
            EventsFor(aggregateId).AddRange(eventData);
        }
    }

    public class SimpleFilePersistenceEngine : IEventPersistance
    {
        #region Fields

        private readonly IEventSerializer _serializer;

        private readonly ConcurrentDictionary<IAggregateIdentity, int> _versionCache = new ConcurrentDictionary<IAggregateIdentity, int>();

        private readonly string _storagePath;

        #endregion

        #region Constructors

        public SimpleFilePersistenceEngine(IEventSerializer serializer, string storagePath)
        {
            _serializer = serializer;
            _storagePath = storagePath;
        }

        #endregion

        public void AppendEvents(IAggregateIdentity aggregateId, IEnumerable<IEvent> eventsToAppend)
        {
            using (var writer = GetWriter(aggregateId))
            {
                foreach (var eventData in eventsToAppend.Select(_serializer.Serialize))
                {
                    writer.Write(eventData.Length);
                    writer.Write(eventData);
                }
            }
        }

        public IEnumerable<IEvent> GetEventsFor(IAggregateIdentity aggregateId, int version)
        {
            var events = new List<IEvent>();
            using (var reader = GetReader(aggregateId))
            {
                while (reader.PeekChar() > -1 && events.Count < version)
                {
                    var dataLength = reader.ReadInt32();
                    var data = reader.ReadBytes(dataLength);
                    events.Add(_serializer.Deserialize(data));
                }
            }

            SetCachedVersion(aggregateId, events.Count);
            return events;
        }

        public int GetVersionFor(IAggregateIdentity aggregateId)
        {
            return _versionCache.GetOrAdd(aggregateId, GetVersionFromFile);
        }

        private void SetCachedVersion(IAggregateIdentity aggregateId, int version)
        {
            _versionCache.AddOrUpdate(aggregateId, version, (i, c) => version);
        }

        private int GetVersionFromFile(IAggregateIdentity aggregateId)
        {
            // TODO : Need a more efficient way to store version. Separate file? End of file?
            var version = 0;
            using (var reader = GetReader(aggregateId))
            {
                while (reader.PeekChar() > -1)
                {
                    var dataLength = reader.ReadInt32();
                    reader.BaseStream.Position += dataLength;
                    version++;
                }
            }

            return version;
        }

        private BinaryReader GetReader(IAggregateIdentity aggregateId)
        {
            var fileStream = File.Open(GetFilePath(aggregateId), FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read);
            return new BinaryReader(fileStream);
        }

        private string GetFilePath(IAggregateIdentity aggregateId)
        {
            var filePath = Path.Combine(_storagePath, String.Concat(aggregateId.GetTag(), aggregateId.GetId()));
            return filePath;
        }

        private BinaryWriter GetWriter(IAggregateIdentity aggregateId)
        {
            var fileStream = File.Open(GetFilePath(aggregateId), FileMode.Append, FileAccess.Write, FileShare.None);
            return new BinaryWriter(fileStream);
        }
    }

    [ContractClassFor(typeof(IEventPersistance))]
    internal abstract class EventPersistanceContract : IEventPersistance
    {
        public void AppendEvents(IAggregateIdentity aggregateId, IEnumerable<IEvent> eventsToAppend)
        {
            Contract.Requires<ArgumentNullException>(aggregateId != null, "aggregateId cannot be null");
            Contract.Requires<ArgumentNullException>(eventsToAppend != null, "eventsToAppend cannot be null");
            Contract.Requires<ArgumentException>(Contract.ForAll(eventsToAppend, e => e != null), "none of the events in eventsToAppend can be null");
        }

        [Pure]
        public IEnumerable<IEvent> GetEventsFor(IAggregateIdentity aggregateId, int version)
        {
            Contract.Requires<ArgumentNullException>(aggregateId != null, "aggregateId cannot be null");
            Contract.Requires<ArgumentOutOfRangeException>(version >= 0, "version cannot be negative");
            throw new NotImplementedException();
        }

        [Pure]
        public int GetVersionFor(IAggregateIdentity aggregateId)
        {
            Contract.Requires<ArgumentNullException>(aggregateId != null, "aggregateId cannot be null");
            throw new NotImplementedException();
        }
    }
}

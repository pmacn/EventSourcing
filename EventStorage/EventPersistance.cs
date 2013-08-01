using EventSourcing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;

namespace EventStorage
{
    [ContractClass(typeof(IEventPersistanceContract))]
    public interface IEventPersistance
    {
        void AppendEvents(IIdentity aggregateId, IEnumerable<IEvent> eventsToAppend);

        IEnumerable<IEvent> GetEventsFor(IIdentity aggregateId);

        int GetVersionFor(IIdentity aggregateId);
    }

    [ContractClassFor(typeof(IEventPersistance))]
    internal abstract class IEventPersistanceContract : IEventPersistance
    {
        public void AppendEvents(IIdentity aggregateId, IEnumerable<IEvent> eventsToAppend)
        {
            Contract.Requires<ArgumentNullException>(aggregateId != null, "aggregateId cannot be null");
            Contract.Requires<ArgumentNullException>(eventsToAppend != null, "eventsToAppend cannot be null");
            Contract.Requires<ArgumentException>(Contract.ForAll(eventsToAppend, e => e != null), "none of the events in eventsToAppend can be null");
        }

        [Pure]
        public IEnumerable<IEvent> GetEventsFor(IIdentity aggregateId)
        {
            Contract.Requires<ArgumentNullException>(aggregateId != null, "aggregateId cannot be null");
            throw new NotImplementedException();
        }

        [Pure]
        public int GetVersionFor(IIdentity aggregateId)
        {
            Contract.Requires<ArgumentNullException>(aggregateId != null, "aggregateId cannot be null");
            throw new NotImplementedException();
        }
    }

    public class InMemoryEventPersistance : IEventPersistance
    {
        private readonly ConcurrentDictionary<IIdentity, List<IEvent>> _events = new ConcurrentDictionary<IIdentity, List<IEvent>>();

        public int GetVersionFor(IIdentity aggregateId)
        {
            return EventsFor(aggregateId).Count;
        }

        public IEnumerable<IEvent> GetEventsFor(IIdentity aggregateId)
        {
            return EventsFor(aggregateId).ToList();
        }

        private List<IEvent> EventsFor(IIdentity aggregateId)
        {
            return _events.GetOrAdd(aggregateId, new List<IEvent>());
        }

        public void AppendEvents(IIdentity aggregateId, IEnumerable<IEvent> eventsToAppend)
        {
            EventsFor(aggregateId).AddRange(eventsToAppend);
        }
    }

    public class SqlEventPersistance : IEventPersistance
    {
        /*
         * Events table structure
         * -------------------------
         * nvarchar  AggregateId
         * nvarchar  AggregateIdTag
         * integer identity Version  // not used as the actual version of the aggregate just for ordering
         * varbinary EventData
         * 
         */

        private const string InsertEventsQuery =
            "INSERT INTO AggregateEvents (AggregateId, AggregateIdTag, EventData) VALUES (@aggregateId, @aggregateIdTag, @eventData);";

        private const string CountEventsQuery =
            "SELECT COUNT(*) FROM AggregateEvents WHERE AggregateId = @aggregateId AND AggregateIdTag = @aggregateIdTag;";

        private const string SelectEventsQuery =
            "SELECT EventData FROM AggregateEvents WHERE AggregateId = @aggregateId AND AggregateIdTag = @aggregateIdTag ORDER BY Version ASC;";

        private readonly string _connectionString;

        private readonly IEventSerializer _serializer;

        public SqlEventPersistance(IEventSerializer serializer, string connectionString)
        {
            Contract.Requires<ArgumentNullException>(serializer != null, "serializer cannot be null");
            Contract.Requires<ArgumentException>(!String.IsNullOrWhiteSpace(connectionString), "connectionString cannot be null, empty or whitespace");
            _serializer = serializer;
            _connectionString = connectionString;
        }

        public void AppendEvents(IIdentity aggregateId, IEnumerable<IEvent> eventsToAppend)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                using (var cmd = new SqlCommand(InsertEventsQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@aggregateId", aggregateId.GetId());
                    cmd.Parameters.AddWithValue("@aggregateIdTag", aggregateId.GetTag());
                    var dataParameter = cmd.Parameters.Add("@eventData", SqlDbType.VarBinary);
                    conn.Open();
                    foreach (var eventData in eventsToAppend.Select(_serializer.Serialize))
                    {
                        dataParameter.Value = eventData;
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        public IEnumerable<IEvent> GetEventsFor(IIdentity aggregateId)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                using (var cmd = new SqlCommand(SelectEventsQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@aggregateId", aggregateId.GetId());
                    cmd.Parameters.AddWithValue("@aggregateIdTag", aggregateId.GetTag());
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        var events = new List<IEvent>();
                        while (reader.Read())
                        {
                            var eventData = reader.GetSqlBinary(reader.GetOrdinal("EventData")).Value;
                            events.Add(_serializer.Deserialize(eventData));
                        }

                        return events;
                    }
                }
            }
        }

        public int GetVersionFor(IIdentity aggregateId)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                using (var cmd = new SqlCommand(CountEventsQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@aggregateId", aggregateId.GetId());
                    cmd.Parameters.AddWithValue("@aggregateIdTag", aggregateId.GetTag());
                    conn.Open();
                    return (int)cmd.ExecuteScalar();
                }
            }
        }

        [ContractInvariantMethod]
        private void InvariantMethod()
        {
            Contract.Invariant(!String.IsNullOrWhiteSpace(_connectionString), "_connectionString cannot be null, empty or whitespace");
            Contract.Invariant(_serializer != null, "_serializer cannot be null");
        }
    }

    public class FileSystemEventPersistance : IEventPersistance
    {
        #region Fields

        private readonly IEventSerializer _serializer;

        private readonly ConcurrentDictionary<IIdentity, int> _versionCache = new ConcurrentDictionary<IIdentity, int>();

        private readonly string _storagePath;

        #endregion

        #region Constructors

        public FileSystemEventPersistance(IEventSerializer serializer, string storagePath)
        {
            _serializer = serializer;
            _storagePath = storagePath;
        }

        #endregion

        public void AppendEvents(IIdentity aggregateId, IEnumerable<IEvent> eventsToAppend)
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

        public IEnumerable<IEvent> GetEventsFor(IIdentity aggregateId)
        {
            var events = new List<IEvent>();
            using (var reader = GetReader(aggregateId))
            {
                while (reader.PeekChar() > -1)
                {
                    var dataLength = reader.ReadInt32();
                    var data = reader.ReadBytes(dataLength);
                    events.Add(_serializer.Deserialize(data));
                }
            }

            SetCachedVersion(aggregateId, events.Count);
            return events;
        }

        public int GetVersionFor(IIdentity aggregateId)
        {
            return _versionCache.GetOrAdd(aggregateId, GetVersionFromFile);
        }

        private void SetCachedVersion(IIdentity aggregateId, int version)
        {
            _versionCache.AddOrUpdate(aggregateId, version, (i, c) => version);
        }

        private int GetVersionFromFile(IIdentity aggregateId)
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

        private BinaryReader GetReader(IIdentity aggregateId)
        {
            var fileStream = File.Open(GetFilePath(aggregateId), FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read);
            return new BinaryReader(fileStream);
        }

        private string GetFilePath(IIdentity aggregateId)
        {
            var filePath = Path.Combine(_storagePath, String.Concat(aggregateId.GetTag(), aggregateId.GetId()));
            return filePath;
        }

        private BinaryWriter GetWriter(IIdentity aggregateId)
        {
            var fileStream = File.Open(GetFilePath(aggregateId), FileMode.Append, FileAccess.Write, FileShare.None);
            return new BinaryWriter(fileStream);
        }
    }
}
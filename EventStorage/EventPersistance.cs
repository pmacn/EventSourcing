using EventSourcing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;

namespace ELI.EventStore
{
    public interface IEventPersistance
    {
        void AppendEvents(IIdentity aggregateId, IEnumerable<IEvent> eventsToAppend);

        [Pure]
        IEnumerable<IEvent> GetEventsFor(IIdentity aggregateId);

        [Pure]
        long GetVersionFor(IIdentity aggregateId);
    }

    public class InMemoryEventPersistance : IEventPersistance
    {
        ConcurrentDictionary<IIdentity, List<IEvent>> _events = new ConcurrentDictionary<IIdentity, List<IEvent>>();

        public long GetVersionFor(IIdentity aggregateId)
        {
            Contract.Requires(aggregateId != null, "aggregateId cannot be null");
            return EventsFor(aggregateId).Count;
        }

        public IEnumerable<IEvent> GetEventsFor(IIdentity aggregateId)
        {
            Contract.Requires(aggregateId != null, "aggregateId cannot be null");
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
            Contract.Requires(serializer != null, "serializer cannot be null");
            Contract.Requires(!String.IsNullOrWhiteSpace(connectionString), "connectionString cannot be null, empty or whitespace");
            _serializer = serializer;
            _connectionString = connectionString;
        }

        public void AppendEvents(IIdentity aggregateId, IEnumerable<IEvent> eventsToAppend)
        {
            Contract.Requires(aggregateId != null, "aggregateId cannot be null");
            Contract.Requires(eventsToAppend != null, "eventsToAppend cannot be null");

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
            Contract.Requires(aggregateId != null, "aggregateId cannot be null");

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

        public long GetVersionFor(IIdentity aggregateId)
        {
            Contract.Requires(aggregateId != null, "aggregateId cannot be null");

            using (var conn = new SqlConnection(_connectionString))
            {
                using (var cmd = new SqlCommand(CountEventsQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@aggregateId", aggregateId.GetId());
                    cmd.Parameters.AddWithValue("@aggregateIdTag", aggregateId.GetTag());
                    conn.Open();
                    return (long)cmd.ExecuteScalar();
                }
            }
        }
    }

    public class FileSystemEventPersistance : IEventPersistance
    {
        #region Fields

        private readonly IEventSerializer _serializer;

        private readonly ConcurrentDictionary<IIdentity, long> _versionCache = new ConcurrentDictionary<IIdentity, long>();

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
            long dataLength = 0;
            using (var reader = GetReader(aggregateId))
            {
                while (reader.PeekChar() > -1)
                {
                    dataLength = reader.ReadInt64();
                    var data = reader.ReadBytes((int)dataLength); // TODO: make this handle chunks longer than Int32.MaxValue
                    events.Add(_serializer.Deserialize(data));
                }
            }

            SetCachedVersion(aggregateId, events.LongCount());
            return events;
        }

        public long GetVersionFor(IIdentity aggregateId)
        {
            return _versionCache.GetOrAdd(aggregateId, GetVersionFromFile);
        }

        private void SetCachedVersion(IIdentity aggregateId, long version)
        {
            _versionCache.AddOrUpdate(aggregateId, version, (i, c) => version);
        }

        private long GetVersionFromFile(IIdentity aggregateId)
        {
            // TODO : Need a more efficient way to store version. Separate file? End of file?
            long version = 0;
            long dataLength = 0;
            using (var reader = GetReader(aggregateId))
            {
                while (reader.PeekChar() > -1)
                {
                    dataLength = reader.ReadInt64();
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
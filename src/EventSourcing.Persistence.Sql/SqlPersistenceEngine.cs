using EventSourcing.Serialization;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventSourcing.Persistence.SqlServer
{

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
            "SELECT TOP @version EventData FROM AggregateEvents WHERE AggregateId = @aggregateId AND AggregateIdTag = @aggregateIdTag ORDER BY Version ASC;";

        private readonly string _connectionString;

        private readonly IEventSerializer _serializer;

        public SqlEventPersistance(IEventSerializer serializer, string connectionString)
        {
            Contract.Requires<ArgumentNullException>(serializer != null, "serializer cannot be null");
            Contract.Requires<ArgumentException>(!String.IsNullOrWhiteSpace(connectionString), "connectionString cannot be null, empty or whitespace");
            _serializer = serializer;
            _connectionString = connectionString;
        }

        public void AppendEvents(IAggregateIdentity aggregateId, IEnumerable<IEvent> eventsToAppend)
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

        public IEnumerable<IEvent> GetEventsFor(IAggregateIdentity aggregateId, int version)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                using (var cmd = new SqlCommand(SelectEventsQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@aggregateId", aggregateId.GetId());
                    cmd.Parameters.AddWithValue("@aggregateIdTag", aggregateId.GetTag());
                    cmd.Parameters.AddWithValue("@version", version);
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

        public int GetVersionFor(IAggregateIdentity aggregateId)
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

}

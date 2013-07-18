using EventSourcing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace EventSourcing.Example
{
    public class ExampleApplicationService : ApplicationService<ExampleId>
    {
        private readonly IEventStore _store;

        public ExampleApplicationService(IEventStore store) { _store = store; }

        public void When(OpenExample c) { Update(c.Id, e => e.Open(c.Id)); }

        private void Update(ExampleId aggregateId, Action<ExampleAggregate> updateAction)
        {
            var stream = _store.GetEventStreamFor(aggregateId);
            var agg = new ExampleAggregate(stream.Events);
            try
            {
                updateAction(agg);
            }
            catch (DomainError error)
            {
                // handle the error here
            }

            _store.AppendEventsToStream(aggregateId, stream.StreamVersion, agg.UncommittedEvents);
        }
    }
}


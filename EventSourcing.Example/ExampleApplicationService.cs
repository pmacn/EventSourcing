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
        private readonly Repository _repository;

        public ExampleApplicationService(Repository repository) { _repository = repository; }

        public void When(OpenExample c) { Update(c.Id, c.ExpectedVersion, e => e.Open(c.Id)); }

        private void Update(ExampleId aggregateId, long expectedVersion, Action<ExampleAggregate> updateAction)
        {
            var agg = _repository.GetById<ExampleAggregate>(aggregateId);
            try
            {
                updateAction(agg);
            }
            catch (DomainError error)
            {
                // handle the error here
            }

            _repository.Save(agg, expectedVersion);
        }
    }
}


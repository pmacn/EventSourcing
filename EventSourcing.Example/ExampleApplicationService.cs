using System;

namespace EventSourcing.Example
{
    public class ExampleApplicationService : ApplicationService<ExampleId>
    {
        private readonly Repository _repository;

        public ExampleApplicationService(Repository repository) { _repository = repository; }

        public void When(OpenExample c) { Update(c.Id, e => e.Open(c.Id)); }

        private void Update(ExampleId aggregateId, Action<ExampleAggregate> updateAction)
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

            _repository.Save(agg);
        }
    }
}


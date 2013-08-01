using System;

namespace EventSourcing.Example
{
    public class ExampleApplicationService : ApplicationService<ExampleId>
    {
        private readonly Repository _repository;

        public ExampleApplicationService(Repository repository, IDomainErrorRouter errorRouter)
            : base(errorRouter)
        {
            _repository = repository;
        }

        public void When(OpenExample c) { Update(c.Id, e => e.Open(c.Id)); }

        private void Update(ExampleId aggregateId, Action<ExampleAggregate> updateAction)
        {
            var agg = _repository.GetById<ExampleAggregate>(aggregateId);
            updateAction(agg);
            _repository.Save(agg);
        }
    }
}

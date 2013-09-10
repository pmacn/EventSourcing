using System;
using EventSourcing.ApplicationService;
using EventSourcing.Persistence;

namespace EventSourcing.Example
{
    public class ExampleApplicationService : ApplicationService<ExampleId>
    {
        private readonly Repository _repository;

        public ExampleApplicationService(Repository repository, IDomainErrorRouter errorRouter)
            : base(repository, errorRouter)
        {
            _repository = repository;
        }

        public void When(OpenExample c) { Update<ExampleAggregate, ExampleId>(c, e => e.Open(c.Id)); }

    }

    public class ExampleWithStateApplicationService : ApplicationService<ExampleId>
    {
        private readonly Repository _repository;

        public ExampleWithStateApplicationService(Repository repository, IDomainErrorRouter errorRouter)
            : base(repository, errorRouter)
        {
            _repository = repository;
        }

        public void When(OpenExample c) { Update(c, e => e.Open(c.Id)); }

        private void Update(ICommand<ExampleId> command, Action<AggregateWithStateClass> updateAction)
        {
            var agg = _repository.GetById<AggregateWithStateClass>(command.Id);
            updateAction(agg);
            _repository.Save(agg);
        }
    }
}


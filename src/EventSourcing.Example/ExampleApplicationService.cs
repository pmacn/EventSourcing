using System;
using EventSourcing.ApplicationService;
using EventSourcing.Persistence;

namespace EventSourcing.Example
{
    public class ExampleApplicationService : ApplicationService<ExampleId>
    {
        public ExampleApplicationService(Repository repository, IDomainErrorRouter errorRouter)
            : base(repository, errorRouter)
        { }

        public void When(OpenExample c) { Update<ExampleAggregate, ExampleId>(c, e => e.Open(c.Id)); }
    }

    public class ExampleWithStateApplicationService : ApplicationService<ExampleId>
    {
        public ExampleWithStateApplicationService(Repository repository, IDomainErrorRouter errorRouter)
            : base(repository, errorRouter)
        { }

        public void When(OpenExample c) { Update<ExampleAggregate, ExampleId>(c, e => e.Open(c.Id)); }
    }
}


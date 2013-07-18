using ELI.EventSourcing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace EventSourcing.Example
{
    public class ExampleId : AbstractIdentity<int>
    {
        public override int Id { get; protected set; }

        public override string GetTag() { return "Example"; }
    }

    public class ExampleState : AggregateState<ExampleId>
    {
        public ExampleState(IEnumerable<IEvent> history)
            : base(history) { }

        public void When(ExampleOpened e) { Id = e.Id; }
    }

    public class ExampleAggregate : AggregateRoot<ExampleId, ExampleState>
    {
        public ExampleAggregate(IEnumerable<IEvent> history)
            : base(new ExampleState(history)) { }

        public void Open(ExampleId id)
        {
            if (State.Id != null)
                throw DomainError.Named("example-already-opened", "");
            ApplyChange(new ExampleOpened(id, DateTime.Now));
        }
    }
}
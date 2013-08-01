using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace EventSourcing.Example
{
    public class ExampleId : AbstractIdentity<int>
    {
        public ExampleId(int id)
        {
            Id = id;
        }

        public override int Id { get; protected set; }

        public override string GetTag() { return "Example"; }
    }

    public class ExampleState : AggregateState<ExampleId>
    {
        public ExampleState(IEnumerable<IEvent> history)
            : base(history) { }

        public void When(ExampleOpened e)
        {
            Contract.Requires(e != null, "event cannot be null");
            Id = e.Id;
        }
    }

    public class ExampleAggregate : AggregateRoot<ExampleId>
    {
        public ExampleAggregate(IEnumerable<IEvent> history)
        {
            Contract.Requires(history != null, "history cannot be null");
            State = new ExampleState(history);
        }

        public void Open(ExampleId id)
        {
            if (id == null)
                throw DomainError.Named("invalid-aggregate-id", "null is not a valid id for Example");

            if (State.Id != null)
                throw DomainError.Named("example-already-opened", "");
            ApplyChange(new ExampleOpened(id, DateTime.Now));
        }

        public ExampleState State { get; private set; }

        protected override IAggregateState<ExampleId> GenericState
        {
            get { return State; }
        }
    }
}
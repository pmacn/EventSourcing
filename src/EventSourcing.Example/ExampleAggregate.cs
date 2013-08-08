using System;
using System.Diagnostics.Contracts;

namespace EventSourcing.Example
{
    [Serializable]
    public class ExampleId : AbstractAggregateIdentity<int>
    {
        public ExampleId(int id)
        {
            Id = id;
        }

        public override sealed int Id { get; protected set; }

        public override string GetTag() { return "Example"; }
    }

    public class ExampleState : AggregateState<ExampleId>
    {
        public void When(ExampleOpened e)
        {
            Contract.Requires(e != null, "event cannot be null");
            Id = e.Id;
        }
    }

    public sealed class ExampleAggregate : AggregateRoot<ExampleId>
    {
        public override ExampleId Id { get; protected set; }

        public void Open(ExampleId id)
        {
            if (id == null)
                throw DomainError.Named("invalid-aggregate-id", "null is not a valid id for Example");

            if (Id != null)
                throw DomainError.Named("example-already-opened", "");

            ApplyChange(new ExampleOpened(id, DateTime.Now));
        }

        public void When(ExampleOpened e)
        {
            Id = e.Id;
        }
    }

    public sealed class AggregateWithStateClass : AggregateRoot<ExampleId>
    {
        private readonly ExampleState _state;

        public AggregateWithStateClass()
            : this(new ExampleState())
        { }

        private AggregateWithStateClass(ExampleState state)
            : base(state)
        {
            _state = state;
        }

        public override ExampleId Id
        {
            get { return _state.Id; }
            protected set { throw new NotSupportedException(); }
        }

        public void Open(ExampleId id)
        {
            if (id == null)
                throw DomainError.Named("invalid-aggregate-id", "null is not a valid id for Example");

            if (Id != null)
                throw DomainError.Named("example-already-opened", "");

            ApplyChange(new ExampleOpened(id, DateTime.Now));
        }
    }
}
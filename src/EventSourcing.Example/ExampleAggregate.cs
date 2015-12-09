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

    public sealed class ExampleAggregate : AggregateRoot
    {
        public override IAggregateIdentity Id { get; protected set; }

        public ExampleAggregate()
            : this(new ConventionEventRouter())
        { }

        private ExampleAggregate(ConventionEventRouter eventRouter)
            : base(eventRouter)
        {
            eventRouter.Register(this);
        }

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

    public sealed class AggregateWithStateClass : AggregateRoot
    {
        private readonly ExampleState _state;

        public AggregateWithStateClass()
            : this(new ExampleState(), new ConventionEventRouter())
        { }

        private AggregateWithStateClass(ExampleState state, ConventionEventRouter eventRouter)
            : base(eventRouter)
        {
            _state = state;
            eventRouter.Register(_state);
        }

        public override IAggregateIdentity Id
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
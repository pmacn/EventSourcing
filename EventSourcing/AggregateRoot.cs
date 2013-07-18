
namespace ELI.EventSourcing
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Collections;
    using System.Diagnostics.Contracts;
    using System.Runtime.Serialization;

    public interface IAggregateRoot
    {
        long Version { get; }

        IUncommittedEvents UncommittedEvents { get; }
    }

    public interface IAggregateRoot<TIdentity> : IAggregateRoot
        where TIdentity : IIdentity
    {
        TIdentity Id { get; }
    }

    public interface IUncommittedEvents : IEnumerable<IEvent>
    {
        void MarkAsCommitted();
    }

    // It does feel a little icky to have two type parameters on this
    // Another option would be to leave the class slightly more open
    // and with some more abstract properties/methods, maybe something
    // that could be templated?
    public abstract class AggregateRoot<TIdentity, TState> : IAggregateRoot<TIdentity>
        where TIdentity : IIdentity
        where TState : IAggregateState<TIdentity>
    {
        private readonly UncommittedEvents _uncommittedEvents = new UncommittedEvents();

        public AggregateRoot(TState state)
        {
            Contract.Requires(State != null, "state cannot be null");
            State = state;
        }

        protected TState State { get; private set; }

        public TIdentity Id { get { return State.Id; } }

        public long Version { get { return State.Version; } }

        public IUncommittedEvents UncommittedEvents { get { return _uncommittedEvents; } }

        protected void ApplyChange(IEvent eventToApply)
        {
            Contract.Requires<ArgumentNullException>(eventToApply != null, "eventToApply cannot be null");
            ApplyChange(eventToApply, true);
        }

        private void ApplyChange(IEvent eventToApply, bool isNew)
        {
            Contract.Requires(State != null, "State cannot be null");
            State.ApplyChange(eventToApply);
            if (isNew)
                _uncommittedEvents.Append(eventToApply);
        }
    }

    internal class UncommittedEvents : IUncommittedEvents
    {
        private List<IEvent> _events = new List<IEvent>();

        public void Append(IEvent eventToAdd) { _events.Add(eventToAdd); }

        public void MarkAsCommitted() { _events.Clear(); }

        public IEnumerator<IEvent> GetEnumerator() { return _events.GetEnumerator(); }

        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
    }

    #region Aggregate Example

    internal class ExampleAggregate : AggregateRoot<ExampleId, ExampleState>
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

    internal class ExampleId : AbstractIdentity<int>
    {
        public override int Id { get; protected set; }

        public override string GetTag() { return "Example"; }
    }

    internal class ExampleState : AggregateState<ExampleId>
    {
        public ExampleState(IEnumerable<IEvent> history)
            : base(history) { }

        public void When(ExampleOpened e) { Id = e.Id; }
    }

    [DataContract(Namespace="ExampleContractNamespace")]
    internal class ExampleOpened : IEvent<ExampleId>
    {
        public ExampleOpened(ExampleId id, DateTime openingDate)
        {
            Id = id;
            OpeningDate = openingDate;
        }

        [DataMember(Order=1)] public ExampleId Id { get; private set; }

        [DataMember(Order=2)] public DateTime OpeningDate { get; private set; }
    }

    [DataContract(Namespace="ExampleContractNamespace")]
    internal class OpenExample : ICommand<ExampleId>
    {
        public OpenExample(ExampleId id)
        {
            Id = id;
        }

        [DataMember(Order=1)] public ExampleId Id { get; set; }
    }

    internal class ExampleApplicationService : ApplicationService<ExampleId>
    {
        private readonly IEventStore _store;

        public ExampleApplicationService(IEventStore store)
        {
            _store = store;
        }

        public void When(OpenExample c)
        {
            Update(c.Id, e => e.Open(c.Id));
        }

        private void Update(ExampleId aggregateId, Action<ExampleAggregate> updateAction)
        {
            var stream = _store.GetEventStreamFor(aggregateId);
            var agg = new ExampleAggregate(stream.Events);
            updateAction(agg);
            _store.AppendEventsToStream(aggregateId, stream.StreamVersion, agg.UncommittedEvents);
        }
    }

    #endregion
}

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


}
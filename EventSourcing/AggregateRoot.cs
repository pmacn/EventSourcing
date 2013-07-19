
namespace EventSourcing
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;

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


    /// <summary>
    /// A generic aggregate root with wiring to apply events and keep uncommitted events
    /// </summary>
    /// <typeparam name="TIdentity"></typeparam>
    public abstract class AggregateRoot<TIdentity> : IAggregateRoot<TIdentity>
        where TIdentity : IIdentity
    {
        private readonly UncommittedEvents _uncommittedEvents = new UncommittedEvents();

        protected abstract IAggregateState<TIdentity> GenericState { get; }

        public TIdentity Id { get { return GenericState.Id; } }

        public long Version { get { return GenericState.Version; } }

        public IUncommittedEvents UncommittedEvents { get { return _uncommittedEvents; } }

        protected void ApplyChange(IEvent eventToApply)
        {
            Contract.Requires<ArgumentNullException>(eventToApply != null, "eventToApply cannot be null");
            ApplyChange(eventToApply, true);
        }

        private void ApplyChange(IEvent eventToApply, bool isNew)
        {
            Contract.Requires(GenericState != null, "state cannot be null");
            GenericState.ApplyChange(eventToApply);
            if (isNew)
                _uncommittedEvents.Append(eventToApply);
        }
    }

    public interface IUncommittedEvents : IEnumerable<IEvent>
    {
        void MarkAsCommitted();
    }

    internal class UncommittedEvents : IUncommittedEvents
    {
        private List<IEvent> _events = new List<IEvent>();

        public void Append(IEvent eventToAdd)
        {
            Contract.Requires<ArgumentNullException>(eventToAdd != null, "eventToAdd cannot be null");
            _events.Add(eventToAdd);
        }

        public void MarkAsCommitted() { _events.Clear(); }

        [Pure]
        public IEnumerator<IEvent> GetEnumerator() { return _events.GetEnumerator(); }

        [Pure]
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
    }
}
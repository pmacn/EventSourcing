
using System.Runtime.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;

namespace EventSourcing
{
    [ContractClass(typeof(AggregateRootContract))]
    public interface IAggregateRoot
    {
        int Version { get; }

        IUncommittedEvents UncommittedEvents { get; }

        void LoadFrom(IEnumerable<IEvent> history);
    }

    public interface IAggregateRoot<out TIdentity> : IAggregateRoot
        where TIdentity : IAggregateIdentity
    {
        TIdentity Id { get; }
    }
    
    /// <summary>
    /// A generic aggregate root with wiring to apply events and keep uncommitted events
    /// </summary>
    /// <typeparam name="TIdentity"></typeparam>
    public abstract class AggregateRoot<TIdentity> : IAggregateRoot<TIdentity>
        where TIdentity : IAggregateIdentity
    {
        private readonly UncommittedEvents _uncommittedEvents = new UncommittedEvents();

        private readonly IEventRouter _eventRouter;

        protected AggregateRoot()
            : this(new ConventionEventRouter()) { }

        protected AggregateRoot(IEventRouter eventRouter)
        {
            Contract.Requires<ArgumentNullException>(eventRouter != null, "eventRouter cannot be null");

            _eventRouter = eventRouter;
            _eventRouter.Register(GetStateObject());
        }

        // HACK: Feels quite smelly, try to come up with a better solution
        protected virtual object GetStateObject() { return this; }

        public abstract TIdentity Id { get; protected set; }

        public int Version { get; private set; }

        public IUncommittedEvents UncommittedEvents { get { return _uncommittedEvents; } }

        public void LoadFrom(IEnumerable<IEvent> history)
        {
            foreach (var e in history)
            {
                ApplyChange(e, false);
            }
        }

        protected void ApplyChange(IEvent eventToApply)
        {
            Contract.Requires<ArgumentNullException>(eventToApply != null, "eventToApply cannot be null");

            ApplyChange(eventToApply, true);
        }

        private void ApplyChange(IEvent eventToApply, bool isNew)
        {
            _eventRouter.Route(eventToApply);
            Version++;
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
        private readonly List<IEvent> _events = new List<IEvent>();

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

    #region Contract classes

    [ContractClassFor(typeof(IAggregateRoot))]
    internal abstract class AggregateRootContract : IAggregateRoot
    {
        public int Version
        {
            get { throw new NotImplementedException(); }
        }

        public IUncommittedEvents UncommittedEvents
        {
            get { throw new NotImplementedException(); }
        }

        public void LoadFrom(IEnumerable<IEvent> history)
        {
            Contract.Requires<ArgumentNullException>(history != null, "history cannot be null");
        }

        [ContractInvariantMethod]
        private void ContractInvariant()
        {
            Contract.Invariant(Version >= 0, "Version cannot be negative");
            Contract.Invariant(UncommittedEvents != null, "UncommittedEvents cannot be null");
        }
    }

    #endregion
}
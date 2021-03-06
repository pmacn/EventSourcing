﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;

namespace EventSourcing
{
    [ContractClass(typeof(AggregateRootContract))]
    public interface IAggregateRoot
    {
        IAggregateIdentity Id { get; }

        int Version { get; }

        IUncommittedEvents UncommittedEvents { get; }

        void LoadFrom(IEnumerable<IEvent> history);
    }
    
    /// <summary>
    /// A generic aggregate root with wiring to apply events and keep uncommitted events
    /// </summary>
    /// <typeparam name="TIdentity"></typeparam>
    public abstract class AggregateRoot : IAggregateRoot
    {
        private readonly UncommittedEvents _uncommittedEvents = new UncommittedEvents();

        private readonly IEventRouter _eventRouter;

        protected AggregateRoot(IEventRouter eventRouter)
        {
            Contract.Requires<ArgumentNullException>(eventRouter != null, "eventRouter");

            _eventRouter = eventRouter;
        }

        public abstract IAggregateIdentity Id { get; protected set; }

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
        public IAggregateIdentity Id
        {
            get { throw new NotImplementedException(); }
        }

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
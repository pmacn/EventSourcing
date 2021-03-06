﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventSourcing
{
    public abstract class Entity
    {
        private readonly IEventRouter _eventRouter;

        protected readonly Action<IEvent> ApplyChange;

        protected Entity(IEventRouter eventRouter, Action<IEvent> applyChange)
        {
            Contract.Requires<ArgumentNullException>(applyChange != null, "recordEvent");

            _eventRouter = eventRouter;
            ApplyChange = applyChange;
        }

        public void Apply(IEvent @event)
        {
            _eventRouter.Route(@event);
        }
    }

    public abstract class AggregateRootEntity<TIdentity>
        where TIdentity : class, IAggregateIdentity
    {
        private readonly UncommittedEvents _uncommittedEvents = new UncommittedEvents();

        private readonly IEventRouter _eventRouter;

        protected AggregateRootEntity(IEventRouter eventRouter)
        {
            Contract.Requires<ArgumentNullException>(eventRouter != null, "eventRouter");
            _eventRouter = eventRouter;
        }

        public TIdentity Id { get; protected set; }

        public int Version { get; private set; }

        public IUncommittedEvents UncommittedEvents { get { return _uncommittedEvents; } }

        protected void ApplyChange(IEvent @event)
        {
            Contract.Requires<ArgumentNullException>(@event != null, "@event");
            ApplyChange(@event, true);
        }

        private void ApplyChange(IEvent @event, bool isNew)
        {
            _eventRouter.Route(@event);
            Version++;
            if(isNew)
                _uncommittedEvents.Append(@event);
        }

        public void LoadFrom(IEnumerable<IEvent> history)
        {
            Contract.Requires<ArgumentNullException>(history != null, "history");
            Contract.Requires<ArgumentNullException>(Contract.ForAll(history, e => e != null), "event in history");

            foreach (var @event in history)
            {
                
                ApplyChange(@event, false);
            }
        }
    }
}
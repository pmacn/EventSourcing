using System;
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

        private readonly Action<IEvent> _recordEvent;

        protected Entity(IEventRouter eventRouter, Action<IEvent> recordEvent)
        {
            Contract.Requires<ArgumentNullException>(eventRouter != null, "eventRouter");
            Contract.Requires<ArgumentNullException>(recordEvent != null, "recordEvent");

            _eventRouter = eventRouter;
            _recordEvent = recordEvent;
        }

        public void Apply(IEvent @event)
        {
            ApplyChange(@event, false);
        }

        protected void ApplyChange(IEvent @event)
        {
            Contract.Requires<ArgumentNullException>(@event != null, "@event");
            ApplyChange(@event, true);
        }

        private void ApplyChange(IEvent @event, bool shouldRecordEvent)
        {
            Contract.Requires<ArgumentNullException>(@event != null, "@event");
            _eventRouter.Route(@event);
            if (shouldRecordEvent)
                _recordEvent(@event);
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
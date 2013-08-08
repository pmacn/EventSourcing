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

        private Action<IEvent> _recordEvent;
        
        public Entity(IEventRouter eventRouter, Action<IEvent> recordEvent, object stateObject = null)
        {
            Contract.Requires<ArgumentNullException>(eventRouter != null, "eventRouter");
            Contract.Requires<ArgumentNullException>(recordEvent != null, "recordEvent");

            _eventRouter = eventRouter;
            _recordEvent = recordEvent;
        }

        public void Apply(IEvent eventToApply)
        {
            ApplyChange(eventToApply, false);
        }

        protected void ApplyChange(IEvent eventToApply)
        {
            ApplyChange(eventToApply, true);
        }

        private void ApplyChange(IEvent eventToApply, bool shouldRecordEvent)
        {
            Contract.Requires<ArgumentNullException>(eventToApply != null, "eventToApply");
            _eventRouter.Route(eventToApply);
            if (shouldRecordEvent)
                _recordEvent(eventToApply);
        }
    }

    public abstract class AggregateRootEntity<TIdentity> where TIdentity : class, IAggregateIdentity
    {

    }
}
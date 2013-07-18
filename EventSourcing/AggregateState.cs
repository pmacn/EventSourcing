using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;

namespace EventSourcing
{
    public interface IAggregateState<TIdentity>
    where TIdentity : IIdentity
    {
        TIdentity Id { get; set; }

        long Version { get; set; }

        void ApplyChange(IEvent eventToApply);
    }

    public abstract class AggregateState<TIdentity> : IAggregateState<TIdentity>
        where TIdentity : IIdentity
    {
        public AggregateState(IEnumerable<IEvent> history)
        {
            Contract.Requires<ArgumentNullException>(history != null, "history cannot be null");

            foreach (var @event in history)
                ApplyChange(@event);
        }

        public TIdentity Id { get; set; }

        public long Version { get; set; }

        public void ApplyChange(IEvent eventToApply)
        {
            Version++;
            ((dynamic)this).When((dynamic)eventToApply);
        }
    }
}

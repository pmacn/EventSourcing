using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;

namespace EventSourcing
{
    [ContractClass(typeof(IAggregateStateContract<>))]
    public interface IAggregateState<TIdentity>
        where TIdentity : IIdentity
    {
        TIdentity Id { get; set; }

        long Version { get; set; }

        void ApplyChange(IEvent eventToApply);
    }

    [ContractClassFor(typeof(IAggregateState<>))]
    internal abstract class IAggregateStateContract<TIdentity> : IAggregateState<TIdentity>
        where TIdentity : IIdentity
    {
        public TIdentity Id
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public long Version
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public void ApplyChange(IEvent eventToApply)
        {
            Contract.Requires<ArgumentNullException>(eventToApply != null, "eventToApply cannot be null");
        }

        [ContractInvariantMethod]
        private void InvariantMethod()
        {
            Contract.Invariant(Version >= 0, "Version cannot be negative");
        }
    }

    public abstract class AggregateState<TIdentity> : IAggregateState<TIdentity>
        where TIdentity : IIdentity
    {
        protected AggregateState(IEnumerable<IEvent> history)
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

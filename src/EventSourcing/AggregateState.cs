using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace EventSourcing
{
    [ContractClass(typeof(IAggregateStateContract<>))]
    public interface IAggregateState<TIdentity>
        where TIdentity : IIdentity
    {
        TIdentity Id { get; set; }
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
    }

    public abstract class AggregateState<TIdentity> : IAggregateState<TIdentity>
        where TIdentity : IIdentity
    {
        public AggregateState()
        {

        }

        public TIdentity Id { get; set; }
    }
}

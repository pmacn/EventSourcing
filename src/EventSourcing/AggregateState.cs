using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace EventSourcing
{
    [ContractClass(typeof(AggregateStateContract<>))]
    public interface IAggregateState<TIdentity>
        where TIdentity : IAggregateIdentity
    {
        TIdentity Id { get; set; }
    }

    public abstract class AggregateState<TIdentity> : IAggregateState<TIdentity>
        where TIdentity : IAggregateIdentity
    {
        protected AggregateState()
        {

        }

        public TIdentity Id { get; set; }
    }

    #region Contract classes

    [ContractClassFor(typeof(IAggregateState<>))]
    internal abstract class AggregateStateContract<TIdentity> : IAggregateState<TIdentity>
        where TIdentity : IAggregateIdentity
    {
        public TIdentity Id
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                Contract.Requires(value != null, "cannot set Id to null");
            }
        }
    }

    #endregion
}

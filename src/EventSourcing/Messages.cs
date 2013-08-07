
using System.Diagnostics.Contracts;

namespace EventSourcing
{
    /// <summary>
    /// An event that happened in the domain
    /// </summary>
    public interface IEvent
    {
    }

    /// <summary>
    /// An <see cref="IEvent"/> that happened to an aggregate with an Id
    /// of type <typeparam name="TIdentity"/>
    /// </summary>
    [ContractClass(typeof(EventContract<>))]
    public interface IEvent<out TIdentity> : IEvent
        where TIdentity : IIdentity
    {
        /// <summary>
        /// Id of the aggregate where the event originated
        /// </summary>
        TIdentity Id { get; }
    }

    /// <summary>
    /// A command to be executed in the domain
    /// </summary>
    public interface ICommand
    {
        int ExpectedVersion { get; }
    }

    /// <summary>
    /// A command to be executed by an aggregate with an Id of type
    /// <typeparamref name="TIdentity"/>
    /// </summary>
    /// <typeparam name="TIdentity"></typeparam>
    [ContractClass(typeof(CommandContract<>))]
    public interface ICommand<out TIdentity> : ICommand
        where TIdentity : IIdentity
    {
        /// <summary>
        /// Id of the aggregate that the command is for
        /// </summary>
        TIdentity Id { get; }
    }

    [ContractClassFor(typeof(IEvent<>))]
    internal abstract  class EventContract<TIdentity> : IEvent<TIdentity>
        where TIdentity : IIdentity
    {
        public TIdentity Id { get { throw new System.NotImplementedException(); } }

        [ContractInvariantMethod]
        private void InvariantMethod()
        {
            Contract.Invariant(null != Id, "Id cannot be null");
        }
    }

    [ContractClassFor(typeof(ICommand<>))]
    internal abstract class CommandContract<TIdentity> : ICommand<TIdentity>
        where TIdentity : IIdentity
    {
        public TIdentity Id
        {
            get { throw new System.NotImplementedException(); }
        }

        public int ExpectedVersion
        {
            get { throw new System.NotImplementedException(); }
        }

        [ContractInvariantMethod]
        private void InvariantMethod()
        {
            Contract.Invariant(Id != null, "Id cannot be null");
            Contract.Invariant(ExpectedVersion >= 0, "ExpectedVersion cannot be negative");
        }
    }
}
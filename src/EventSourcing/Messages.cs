
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
    public interface ICommand<out TIdentity> : ICommand
        where TIdentity : IIdentity
    {
        /// <summary>
        /// Id of the aggregate that the command is for
        /// </summary>
        TIdentity Id { get; }
    }

}
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;
namespace EventSourcing
{
    public interface IApplicationService<TIdentity>
        where TIdentity : IIdentity
    {
        void Execute(ICommand<TIdentity> command);
    }

    public abstract class ApplicationService<TIdentity> : IApplicationService<TIdentity>
        where TIdentity : IIdentity
    {
        private readonly IDomainErrorRouter _errorRouter;

        protected ApplicationService(IDomainErrorRouter errorRouter)
        {
            Contract.Requires<ArgumentNullException>(errorRouter != null, "errorRouter cannot be null");
            _errorRouter = errorRouter;
	    }

        public void Execute(ICommand<TIdentity> command)
        {
            try
            {
                ((dynamic)this).When((dynamic)command);
            }
            catch (DomainError error)
            {
                _errorRouter.Route(error);
            }
        }
    }

    public interface ICommandQueueReader
    {
        bool TryDequeue(out ICommand command);
    }

    public interface ICommandQueueWriter
    {
        void Enqueue(ICommand command);
    }

    public class InMemoryCommandQueue : ICommandQueueReader, ICommandQueueWriter
    {
        private readonly ConcurrentQueue<ICommand> _queue = new ConcurrentQueue<ICommand>();

        public bool TryDequeue(out ICommand command)
        {
            return _queue.TryDequeue(out command);
        }

        public void Enqueue(ICommand command)
        {
            _queue.Enqueue(command);
        }
    }

    public interface IDomainErrorRouter
    {
        void Route(DomainError error);
    }
}
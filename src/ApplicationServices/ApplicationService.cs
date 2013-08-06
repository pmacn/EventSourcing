using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;

namespace EventSourcing
{
    public interface IApplicationService<in TIdentity>
        where TIdentity : IIdentity
    {
        void Execute(ICommand<TIdentity> command);
    }

    public abstract class ApplicationService<TIdentity> : IApplicationService<TIdentity>
        where TIdentity : IIdentity
    {
        private readonly IDomainErrorRouter _errorRouter;

        private readonly Dictionary<Type, Action<ICommand>> _commandHandlers = new Dictionary<Type, Action<ICommand>>();

        protected ApplicationService(IDomainErrorRouter errorRouter)
        {
            Contract.Requires<ArgumentNullException>(errorRouter != null, "errorRouter cannot be null");
            
            _errorRouter = errorRouter;
            SetupCommandHandlers();
        }

        public void Execute(ICommand<TIdentity> command)
        {
            Action<ICommand> handler;
            if (!_commandHandlers.TryGetValue(command.GetType(), out handler))
                throw new CommandHandlerNotFoundException();

            try
            {
                handler(command);
            }
            catch (TargetInvocationException error)
            {
                if(error.InnerException is DomainError)
                    _errorRouter.Route(error.InnerException as DomainError);
                else
                    throw;
            }
        }

        private void SetupCommandHandlers()
        {
            var methods = GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Where(IsCommandHandlerMethod);
            foreach (var method in methods)
            {
                var commandType = method.GetParameters().Single().ParameterType;
                var commandHandler = new Action<object>(c => method.Invoke(this, new [] { c }));
                _commandHandlers.Add(((Type)commandType), commandHandler);
            }
        }

        private static bool IsCommandHandlerMethod(MethodInfo m) { return m.Name == "When" && m.GetParameters().Length == 1 && m.ReturnType == typeof(void); }
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

    [Serializable]
    public class CommandHandlerNotFoundException : Exception
    {
        public CommandHandlerNotFoundException() { }
        public CommandHandlerNotFoundException(string message) : base(message) { }
        public CommandHandlerNotFoundException(string message, Exception inner) : base(message, inner) { }
        protected CommandHandlerNotFoundException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}

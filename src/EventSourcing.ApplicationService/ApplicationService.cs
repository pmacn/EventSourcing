using EventSourcing.ApplicationService.Exceptions;
using EventSourcing.Exceptions;
using EventSourcing.Persistence;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;

namespace EventSourcing.ApplicationService
{
    public interface IApplicationService<in TIdentity>
        where TIdentity : class, IAggregateIdentity
    {
        void Execute(ICommand<TIdentity> command);
    }

    public abstract class ApplicationService<TIdentity> : IApplicationService<TIdentity>
        where TIdentity : class, IAggregateIdentity
    {
        private readonly IDomainErrorRouter _errorRouter;

        private readonly IRepository _repository;

        private readonly Dictionary<Type, Action<ICommand>> _commandHandlers = new Dictionary<Type, Action<ICommand>>();

        protected ApplicationService(IRepository repository, IDomainErrorRouter errorRouter)
        {
            Contract.Requires<ArgumentNullException>(errorRouter != null, "errorRouter cannot be null");
            
            _repository = repository;
            _errorRouter = errorRouter;
            SetupCommandHandlers();
        }

        public void Execute(ICommand<TIdentity> command)
        {
            Action<ICommand> handler;
            handler = _commandHandlers.Single(kvp => kvp.Key.IsAssignableFrom(command.GetType())).Value;
            //if (!_commandHandlers.TryGetValue(command.GetType(), out handler))
            //    throw new CommandHandlerNotFoundException();

            try
            {
                handler(command);
            }
            catch (TargetInvocationException error)
            {
                if (error.InnerException is DomainError)
                    _errorRouter.Route(error.InnerException as DomainError);
                else
                    throw;
            }
        }

        protected void Update<TAggregate, TIdentity>(ICommand<TIdentity> command, Action<TAggregate> action)
            where TAggregate : class, IAggregateRoot<TIdentity>
            where TIdentity : class, IAggregateIdentity
        {
            var aggregate = _repository.GetById<TAggregate>(command.Id);
            if (aggregate.Version != command.ExpectedVersion)
                throw new AggregateConcurrencyException();

            action(aggregate);
            _repository.Save(aggregate);
        }

        private void SetupCommandHandlers()
        {
            var methods = GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Where(IsCommandHandlerMethod);
            foreach (var method in methods)
            {
                var commandType = method.GetParameters().Single().ParameterType;
                var methodInfo = method;
                var commandHandler = new Action<object>(c => methodInfo.Invoke(this, new [] { c }));
                _commandHandlers.Add(commandType, commandHandler);
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

    public class MemoryCommandQueue : ICommandQueueReader, ICommandQueueWriter
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

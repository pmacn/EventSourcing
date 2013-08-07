using System;
using System.Collections.Concurrent;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;

namespace EventSourcing.ApplicationService
{
    /// <summary>
    /// Hosts ApplicationServices and routes commands to the correct one if loaded.
    /// </summary>
    [ContractClass(typeof(ApplicationServiceHostContract))]
    public interface IApplicationServiceHost
    {
        void LoadService<TIdentity>(IApplicationService<TIdentity> service)
            where TIdentity : IIdentity;
    }

    public class DefaultApplicationServiceHost : IApplicationServiceHost
    {
        private readonly ConcurrentDictionary<Type, object> _services = new ConcurrentDictionary<Type, object>();

        private readonly ICommandQueueReader _queueReader;

        private CancellationTokenSource _tokenSource;

        private CancellationToken _token;

        private Task _runningTask;

        private readonly object _mutex = new object();

        public DefaultApplicationServiceHost(ICommandQueueReader queueReader)
        {
            Contract.Requires<ArgumentNullException>(queueReader != null, "queueReader cannot be null.");
            _queueReader = queueReader;
        }

        public void LoadService<TIdentity>(IApplicationService<TIdentity> service)
            where TIdentity : IIdentity
        {
            var idType = typeof(TIdentity);
            if (!_services.TryAdd(idType, service))
                throw new ApplicationServiceAlreadyLoadedException(idType);
        }

        public void Start()
        {
            if (_runningTask != null) return;

            lock (_mutex)
            {
                if (_runningTask != null) return;

                _tokenSource = new CancellationTokenSource();
                _token = _tokenSource.Token;
                _runningTask = Task.Factory.StartNew(Run, _token);
            }
        }

        public void Stop()
        {
            if (_runningTask == null)
                return;

            _tokenSource.Cancel();
            _runningTask.Wait();
        }

        private void Run()
        {
            if (_token.IsCancellationRequested)
                return;

            while (!_token.IsCancellationRequested)
            {
                ICommand command;
                if (_queueReader.TryDequeue(out command))
                {
                    var service = GetServiceFor((dynamic)command);
                    service.Execute((dynamic)command);
                }
                else
                {
                    Thread.Sleep(1);
                }
            }
        }

        private IApplicationService<TIdentity> GetServiceFor<TIdentity>(ICommand<TIdentity> command)
            where TIdentity : IIdentity
        {
            object service;
            if (_services.TryGetValue(typeof(TIdentity), out service))
                return service as IApplicationService<TIdentity>;

            throw new ApplicationServiceNotFoundException("Unable to find applcation service for commands of type " + command.GetType().Name);
        }
    }

    [ContractClassFor(typeof(IApplicationServiceHost))]
    internal abstract class ApplicationServiceHostContract : IApplicationServiceHost
    {
        public void LoadService<TIdentity>(IApplicationService<TIdentity> service)
            where TIdentity : IIdentity
        {
            Contract.Requires<ArgumentNullException>(service != null, "service cannot be null");
            throw new NotImplementedException();
        }
    }
}

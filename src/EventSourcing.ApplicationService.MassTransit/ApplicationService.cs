using System.Diagnostics;
using System.Linq;
using EventSourcing.ApplicationService;
using System;
using MassTransit;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace EventSourcing.ApplcationService.MassTransit
{
    public class MassTransitApplicationServiceHost : IApplicationServiceHost, IDisposable
    {
        private readonly IServiceBus _serviceBus;

        private readonly IDomainErrorRouter _domainErrorRouter;

        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        private readonly List<UnsubscribeAction> _subscriptions = new List<UnsubscribeAction>();

        public MassTransitApplicationServiceHost(IServiceBus serviceBus, IDomainErrorRouter domainErrorRouter)
        {
            Contract.Requires<ArgumentNullException>(serviceBus != null, "serviceBus cannot be null");
            Contract.Requires<ArgumentNullException>(domainErrorRouter != null, "domainErrorRouter cannot be null");

            _serviceBus = serviceBus;
            _domainErrorRouter = domainErrorRouter;
        }

        public void LoadService<TIdentity>(IApplicationService<TIdentity> service) where TIdentity : IIdentity
        {
            var commandType = typeof (ICommand<TIdentity>);
            var subscription = _serviceBus.SubscribeHandler<ICommand<TIdentity>>(SubscriptionMethod);

            _services.Add(commandType, service);
            _subscriptions.Add(subscription);
        }

        private void SubscriptionMethod<TIdentity>(ICommand<TIdentity> command)
            where TIdentity : IIdentity
        {
            object service;
            if(!_services.TryGetValue(command.GetType(), out service) || !(service is IApplicationService<TIdentity>))
                throw new ApplicationServiceNotFoundException();

            try
            {
                (service as IApplicationService<TIdentity>).Execute(command);
            }
            catch (DomainError error)
            {
                _domainErrorRouter.Route(error);
            }
        }

        public void Dispose()
        {
            foreach (var unsubscribe in _subscriptions)
            {
                unsubscribe();
            }

            foreach (var service in _services.OfType<IDisposable>())
            {
                service.Dispose();
            }
        }
    }
}

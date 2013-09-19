using EventSourcing.ApplicationService.Exceptions;
using MassTransit;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;

namespace EventSourcing.ApplicationService.MassTransit
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

        public void LoadService<TIdentity>(IApplicationService<TIdentity> service) where TIdentity : class, IAggregateIdentity
        {
            var identityType = typeof (TIdentity);
            var methods = service.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public).Where(m => m.Name == "When" && m.GetParameters().Count() == 1);

            foreach (var method in methods) {
                var sub = SubscribeHandlerMethod<TIdentity>(service, method);
                _subscriptions.Add(sub);
            }

            _services.Add(identityType, service);
        }

        private UnsubscribeAction SubscribeHandlerMethod<TIdentity>(IApplicationService<TIdentity> service, MethodInfo method)
            where TIdentity : class, IAggregateIdentity
        {
            var commandType = method.GetParameters().Single().ParameterType;
            var handlerType = typeof(Action<>).MakeGenericType(commandType);
            var d = method.CreateDelegate(handlerType, service);
            var subMethod = typeof(HandlerSubscriptionExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static)
                                                                 .Single(m => m.Name == "SubscribeHandler" && m.GetParameters().Count() == 2)
                                                                 .MakeGenericMethod(commandType);
            return subMethod.Invoke(null, new object[] { _serviceBus, d }) as UnsubscribeAction;
        }

        private void SubscriptionMethod<TIdentity>(ICommand<TIdentity> command)
            where TIdentity : class, IAggregateIdentity
        {
            object service;
            if(!_services.TryGetValue(typeof(TIdentity), out service) || !(service is IApplicationService<TIdentity>))
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

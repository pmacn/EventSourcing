using EventSourcing.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;

namespace EventSourcing
{
    [ContractClass(typeof(EventRouterContract))]
    public interface IEventRouter
    {
        void Route(IEvent eventToRoute);
        void Register(object stateObject);
    }

    public class ConventionEventRouter : IEventRouter
    {
        private readonly Dictionary<Type, Action<object>> _routes = new Dictionary<Type, Action<object>>();

        private readonly string _eventHandlerMethodName;

        public ConventionEventRouter()
            : this("When")
        {
            
        }

        public ConventionEventRouter(string eventHandlerMethodName)
        {
            Contract.Requires<ArgumentException>(!String.IsNullOrWhiteSpace(eventHandlerMethodName),
                                                 "eventHandlerMethodName cannot be null, empty or whitespace");

            _eventHandlerMethodName = eventHandlerMethodName;
        }

        public void Route(IEvent eventToRoute)
        {
            Action<object> route;
            if (!_routes.TryGetValue(eventToRoute.GetType(), out route))
                throw new EventHandlerNotFoundException();

            route(eventToRoute);
        }

        public void Register(object stateObject)
        {
            var eventHandlerMethods =
                stateObject.GetType()
                           .GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
                           .Where(IsEventHandlerMethod);

            foreach (var method in eventHandlerMethods)
            {
                var eventType = method.GetParameters().Single().ParameterType;
                var methodInfo = method;
                _routes.Add(eventType, e => methodInfo.Invoke(stateObject, new[] { e }));
            }
        }

        private bool IsEventHandlerMethod(MethodInfo methodInfo)
        {
            if (methodInfo.Name != _eventHandlerMethodName || methodInfo.ReturnType != typeof(void))
                return false;

            var parameters = methodInfo.GetParameters();
            return parameters.Length == 1 && typeof(IEvent).IsAssignableFrom(parameters.Single().ParameterType);
        }
    }

    [ContractClassFor(typeof(IEventRouter))]
    internal abstract class EventRouterContract : IEventRouter
    {
        public void Route(IEvent eventToRoute)
        {
            Contract.Requires<ArgumentNullException>(eventToRoute != null, "eventToRoute cannot be null");
        }

        public void Register(object stateObject)
        {
            Contract.Requires<ArgumentNullException>(stateObject != null, "stateObject cannot be null");
        }
    }
}

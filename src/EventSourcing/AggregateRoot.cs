
namespace EventSourcing
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Reflection;

    public interface IAggregateRoot
    {
        int Version { get; }

        IUncommittedEvents UncommittedEvents { get; }

        void LoadFrom(IEnumerable<IEvent> history);
    }

    public interface IAggregateRoot<out TIdentity> : IAggregateRoot
        where TIdentity : IIdentity
    {
        TIdentity Id { get; }
    }

    /// <summary>
    /// A generic aggregate root with wiring to apply events and keep uncommitted events
    /// </summary>
    /// <typeparam name="TIdentity"></typeparam>
    public abstract class AggregateRoot<TIdentity> : IAggregateRoot<TIdentity>
        where TIdentity : IIdentity
    {
        private readonly UncommittedEvents _uncommittedEvents = new UncommittedEvents();

        private readonly IEventRouter EventRouter;

        public AggregateRoot()
            : this(new ConventionEventRouter())
        { }

        public AggregateRoot(IEventRouter eventRouter)
        {
            Contract.Requires<ArgumentNullException>(eventRouter != null, "eventRouter cannot be null");

            EventRouter = eventRouter;
            EventRouter.Register(GetStateObject());
        }

        // this feels a little icky, and I'm not quite sure why
        public virtual object GetStateObject()
        {
            return this;
        }

        public abstract TIdentity Id { get; protected set; }

        public int Version { get; private set; }

        public IUncommittedEvents UncommittedEvents { get { return _uncommittedEvents; } }

        public void LoadFrom(IEnumerable<IEvent> history)
        {
            foreach (var e in history)
            {
                ApplyChange(e, false);
            }
        }

        protected void ApplyChange(IEvent eventToApply)
        {
            Contract.Requires<ArgumentNullException>(eventToApply != null, "eventToApply cannot be null");

            ApplyChange(eventToApply, true);
        }

        private void ApplyChange(IEvent eventToApply, bool isNew)
        {
            EventRouter.Route(eventToApply);
            Version++;
            if (isNew)
                _uncommittedEvents.Append(eventToApply);
        }
    }

    public interface IEventRouter
    {
        void Route(IEvent eventToRoute);
        void Register(object stateObject);
    }

    public class ConventionEventRouter : IEventRouter
    {
        private readonly Dictionary<Type, Action<object>> _routes = new Dictionary<Type, Action<object>>();

        public void Route(IEvent eventToRoute)
        {
            Action<object> route;
            if(!_routes.TryGetValue(eventToRoute.GetType(), out route))
                throw new HandlerForEventNotFoundException();

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
                _routes.Add(eventType, e => method.Invoke(stateObject, new [] { (dynamic)e }));
            }         
        }

        private static bool IsEventHandlerMethod(MethodInfo m)
        {
            if (m.Name != "When" || m.ReturnType != typeof(void))
                return false;

            var parameters = m.GetParameters();
            return parameters.Length == 1 && typeof(IEvent).IsAssignableFrom(parameters.Single().ParameterType);
        }
    }

    public interface IUncommittedEvents : IEnumerable<IEvent>
    {
        void MarkAsCommitted();
    }

    internal class UncommittedEvents : IUncommittedEvents
    {
        private readonly List<IEvent> _events = new List<IEvent>();

        public void Append(IEvent eventToAdd)
        {
            Contract.Requires<ArgumentNullException>(eventToAdd != null, "eventToAdd cannot be null");
            _events.Add(eventToAdd);
        }

        public void MarkAsCommitted() { _events.Clear(); }

        [Pure]
        public IEnumerator<IEvent> GetEnumerator() { return _events.GetEnumerator(); }

        [Pure]
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
    }

    public class HandlerForEventNotFoundException : System.Exception
    {
    }
}
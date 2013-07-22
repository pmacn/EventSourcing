using System;
using System.Diagnostics.Contracts;

namespace EventSourcing
{
    [ContractClass(typeof(EventPublisherContract))]
    public interface IEventPublisher
    {
        void Publish<TEvent>(TEvent eventToPublish)
            where TEvent : class, IEvent;

        void Publish<TEvent>(TEvent[] eventsToPublish)
            where TEvent : class, IEvent;
    }

    [ContractClassFor(typeof(IEventPublisher))]
    public abstract class EventPublisherContract : IEventPublisher
    {
        void IEventPublisher.Publish<TEvent>(TEvent eventToPublish)
        {
            Contract.Requires<ArgumentNullException>(eventToPublish != null, "eventToPublish cannot be null");
        }

        void IEventPublisher.Publish<TEvent>(TEvent[] eventsToPublish)
        {
            Contract.Requires<ArgumentNullException>(eventsToPublish != null, "eventsToPublish cannot be null");
            Contract.Requires<ArgumentException>(Contract.ForAll(eventsToPublish, e => e != null), "none of the events in eventsToPublish can be null");
        }
    }
}

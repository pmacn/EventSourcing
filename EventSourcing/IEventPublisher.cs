using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;

namespace EventSourcing
{
    [ContractClass(typeof(EventPublisherContract))]
    public interface IEventPublisher
    {
        void Publish<TEvent>(TEvent eventToPublish)
            where TEvent : class, IEvent;

        void Publish<TEvent>(IEnumerable<TEvent> eventsToPublish)
            where TEvent : class, IEvent;
    }

    [ContractClassFor(typeof(IEventPublisher))]
    public abstract class EventPublisherContract : IEventPublisher
    {
        void IEventPublisher.Publish<TEvent>(TEvent eventToPublish)
        {
            Contract.Requires(eventToPublish != null, "eventToPublish cannot be null");
        }

        void IEventPublisher.Publish<TEvent>(IEnumerable<TEvent> eventsToPublish)
        {
            Contract.Requires(eventsToPublish != null, "eventsToPublish cannot be null");
            Contract.Requires(Contract.ForAll(eventsToPublish, e => e != null), "none of the events in eventsToPublish can be null");
        }
    }
}

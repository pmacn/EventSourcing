using System;
using System.Diagnostics.Contracts;

namespace EventSourcing
{
    // TODO : Reconsider the publisher, maybe what's needed is a subscription service on the other end of the storage
    /// <summary>
    /// Publishes <see cref="IEvent"/>s to
    /// </summary>
    [ContractClass(typeof(EventPublisherContract))]
    public interface IEventPublisher
    {
        /// <summary>
        /// Publishes the provided event.
        /// </summary>
        /// <typeparam name="TEvent">Type of the event</typeparam>
        /// <param name="eventToPublish">The event to publish</param>
        void Publish<TEvent>(TEvent eventToPublish)
            where TEvent : class, IEvent;

        /// <summary>
        /// Publishes the provided events.
        /// </summary>
        /// <typeparam name="TEvent">Type of the events</typeparam>
        /// <param name="eventsToPublish">Events to publish.</param>
        /// <exception cref="ArgumentNullException">If eventsToPublish or any of the events in eventsToPublish is null</exception>
        void Publish<TEvent>(TEvent[] eventsToPublish)
            where TEvent : class, IEvent;
    }

    [ContractClassFor(typeof(IEventPublisher))]
    internal abstract class EventPublisherContract : IEventPublisher
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

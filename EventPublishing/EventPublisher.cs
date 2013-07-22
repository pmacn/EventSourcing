using EventSourcing;
using MassTransit;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;

namespace EventPublishing
{
    public class EventPublisher : IEventPublisher
    {
        private readonly IServiceBus _bus;

        public EventPublisher(IServiceBus bus)
        {
            Contract.Requires<ArgumentNullException>(bus != null, "bus cannot be null");
            _bus = bus;
        }

        public void Publish<TEvent>(TEvent eventToPublish)
            where TEvent : class, IEvent
        {
            _bus.Publish(eventToPublish);
        }

        public void Publish<TEvent>(TEvent[] eventsToPublish) where TEvent : class, IEvent
        {
            foreach(var e in eventsToPublish)
                _bus.Publish(e);
        }
    }
}
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
            Contract.Requires(bus != null, "bus cannot be null");
            _bus = bus;
        }

        public void Publish<TEvent>(TEvent eventToPublish)
            where TEvent : IEvent
        {
            Contract.Requires(eventToPublish != null, "eventToPublish cannot be null");

            _bus.Publish((dynamic)eventToPublish);
        }
    }
}
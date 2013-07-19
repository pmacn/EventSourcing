using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EventSourcing
{
    public interface IEventPublisher
    {
        void Publish<TEvent>(TEvent e)
            where TEvent : IEvent;
    }
}

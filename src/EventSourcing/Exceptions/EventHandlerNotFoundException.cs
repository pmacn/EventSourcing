using System;
using System.Runtime.Serialization;

namespace EventSourcing.Exceptions
{

    [Serializable]
    public class EventHandlerNotFoundException : Exception
    {
        public EventHandlerNotFoundException() { }

        public EventHandlerNotFoundException(string message)
            : base(message) { }

        public EventHandlerNotFoundException(string message, Exception inner)
            : base(message, inner) { }

        protected EventHandlerNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
    }
}


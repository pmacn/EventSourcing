using System;
using System.Runtime.Serialization;

namespace EventStorage
{
    [Serializable]
    public class EventSerializationException : Exception
    {
        public EventSerializationException() { }

        public EventSerializationException(string message)
            : base(message) { }

        public EventSerializationException(string message, Exception inner)
            : base(message, inner) { }

        protected EventSerializationException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
    }
}
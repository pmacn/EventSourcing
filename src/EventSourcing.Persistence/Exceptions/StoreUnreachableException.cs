using System;
using System.Runtime.Serialization;

namespace EventSourcing.Persistence.Exceptions
{
    [Serializable]
    public class StoreUnreachableException : Exception
    {
        public StoreUnreachableException() { }

        public StoreUnreachableException(string message)
            : base(message) { }

        public StoreUnreachableException(string message, Exception inner)
            : base(message, inner) { }

        protected StoreUnreachableException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
    }
}
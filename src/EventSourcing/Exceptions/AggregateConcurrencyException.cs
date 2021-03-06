using System;
using System.Runtime.Serialization;

namespace EventSourcing.Exceptions
{
    [Serializable]
    public class AggregateConcurrencyException : Exception
    {
        public AggregateConcurrencyException() { }

        public AggregateConcurrencyException(int expectedVersion, int actualVersion)
            : this(String.Format("Expected version: {0}; Actual version: {1}", expectedVersion, actualVersion)) { }

        public AggregateConcurrencyException(string message)
            : base(message) { }

        public AggregateConcurrencyException(string message, Exception inner)
            : base(message, inner) { }

        protected AggregateConcurrencyException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
    }
}


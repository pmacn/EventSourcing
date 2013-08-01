namespace EventSourcing
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public class AggregateConcurrencyException : Exception
    {
        public AggregateConcurrencyException() { }

        public AggregateConcurrencyException(long expectedVersion, long actualVersion)
            : this(String.Format("Expected version: {0}; Actual version: {1}", expectedVersion, actualVersion)) { }

        public AggregateConcurrencyException(string message)
            : base(message) { }

        public AggregateConcurrencyException(string message, Exception inner)
            : base(message, inner) { }

        protected AggregateConcurrencyException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
    }
}


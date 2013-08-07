namespace EventSourcing
{
    using System;
    using System.Runtime.Serialization;

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

    [Serializable]
    public class HandlerForEventNotFoundException : Exception
    {
        public HandlerForEventNotFoundException() { }

        public HandlerForEventNotFoundException(string message)
            : base(message) { }

        public HandlerForEventNotFoundException(string message, Exception inner)
            : base(message, inner) { }

        protected HandlerForEventNotFoundException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context) { }
    }

    [Serializable]
    public class AggregateConstructionException : Exception
    {
        public AggregateConstructionException() { }
        public AggregateConstructionException(string message) : base(message) { }
        public AggregateConstructionException(string message, Exception inner) : base(message, inner) { }
        protected AggregateConstructionException(
          SerializationInfo info,
          StreamingContext context)
            : base(info, context) { }
    }
}


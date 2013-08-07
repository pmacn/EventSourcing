using System;
using System.Runtime.Serialization;

namespace EventSourcing.Exceptions
{
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


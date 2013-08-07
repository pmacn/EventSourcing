using System;
using System.Runtime.Serialization;

namespace EventSourcing.Persistence.Exceptions
{
    [Serializable]
    public class AggregateDeletedException : Exception
    {
        public AggregateDeletedException() { }

        public AggregateDeletedException(string message)
            : base(message) { }

        public AggregateDeletedException(string message, Exception inner)
            : base(message, inner) { }

        protected AggregateDeletedException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
    }
}


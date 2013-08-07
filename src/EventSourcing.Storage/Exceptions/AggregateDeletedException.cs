using System.Runtime.Serialization;
using EventSourcing;
using EventStore;
using EventStore.ClientAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net;
using System.Collections.Concurrent;
using EventStore.ClientAPI.Exceptions;
using EventSourcing.Serialization;
using EventSourcing.Exceptions;

namespace EventSourcing.Storage.Exceptions
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


using System;
using System.Runtime.Serialization;

namespace EventSourcing.ApplicationService.Exceptions
{
    [Serializable]
    public class ApplicationServiceNotFoundException : Exception
    {
        public ApplicationServiceNotFoundException() { }

        public ApplicationServiceNotFoundException(string message)
            : base(message) { }

        public ApplicationServiceNotFoundException(string message, Exception inner)
            : base(message, inner) { }

        protected ApplicationServiceNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
    }
}


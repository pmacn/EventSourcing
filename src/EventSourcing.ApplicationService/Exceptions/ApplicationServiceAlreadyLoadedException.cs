using System;
using System.Runtime.Serialization;

namespace EventSourcing.ApplicationService.Exceptions
{
    [Serializable]
    public class ApplicationServiceAlreadyLoadedException : Exception
    {
        public ApplicationServiceAlreadyLoadedException() { }

        public ApplicationServiceAlreadyLoadedException(Type type)
            : this(String.Format("An application service for {0} is already loaded.", type.Name)) { }

        public ApplicationServiceAlreadyLoadedException(string message)
            : base(message) { }

        public ApplicationServiceAlreadyLoadedException(string message, Exception inner)
            : base(message, inner) { }

        protected ApplicationServiceAlreadyLoadedException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
    }
}


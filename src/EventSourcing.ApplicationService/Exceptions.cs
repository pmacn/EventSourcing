using System;
using System.Runtime.Serialization;

namespace EventSourcing.ApplicationService
{

    [Serializable]
    public class ApplicationServiceNotFoundException : Exception
    {
        public ApplicationServiceNotFoundException() { }
        public ApplicationServiceNotFoundException(string message) : base(message) { }
        public ApplicationServiceNotFoundException(string message, Exception inner) : base(message, inner) { }
        protected ApplicationServiceNotFoundException(
          SerializationInfo info,
          StreamingContext context)
            : base(info, context) { }
    }

    [Serializable]
    public class ApplicationServiceAlreadyLoadedException : Exception
    {
        public ApplicationServiceAlreadyLoadedException() { }
        public ApplicationServiceAlreadyLoadedException(Type type)
            : this(String.Format("An application service for {0} is already loaded.", type.Name)) { }
        public ApplicationServiceAlreadyLoadedException(string message) : base(message) { }
        public ApplicationServiceAlreadyLoadedException(string message, Exception inner) : base(message, inner) { }
        protected ApplicationServiceAlreadyLoadedException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context) { }
    }
}

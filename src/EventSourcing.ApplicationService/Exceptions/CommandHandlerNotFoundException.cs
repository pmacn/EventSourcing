using System;
using System.Runtime.Serialization;

namespace EventSourcing.ApplicationService.Exceptions
{

    [Serializable]
    public class CommandHandlerNotFoundException : Exception
    {
        public CommandHandlerNotFoundException() { }
        public CommandHandlerNotFoundException(string message) : base(message) { }
        public CommandHandlerNotFoundException(string message, Exception inner) : base(message, inner) { }
        protected CommandHandlerNotFoundException(
          SerializationInfo info,
          StreamingContext context)
            : base(info, context) { }
    }
}

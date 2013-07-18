using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace ELI.EventStore
{
    [Serializable]
    public class StoreUnreachableException : Exception
    {
        public StoreUnreachableException() { }

        public StoreUnreachableException(string message)
            : base(message) { }

        public StoreUnreachableException(string message, Exception inner)
            : base(message, inner) { }

        protected StoreUnreachableException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
    }
}
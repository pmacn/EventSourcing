using System;
using System.Diagnostics.Contracts;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace EventSourcing
{
    [Serializable]
    public class DomainError : Exception
    {
        public DomainError(string message) : base(message) { }

        /// <summary>
        /// Creates a named domain error exception
        /// </summary>
        /// <param name="name">The name to be used to identify this exception.</param>
        /// <param name="format">The format.</param>
        /// <param name="args">The arguments.</param>
        /// <returns></returns>
        public static DomainError Named(string name, string format, params object[] args)
        {
            Contract.Requires<ArgumentException>(!String.IsNullOrWhiteSpace(name), "name cannot be null, empty or whitespace");

            return new DomainError(string.Format(format, args))
            {
                Name = name
            };
        }

        public string Name { get; private set; }

        public DomainError(string message, Exception inner) : base(message, inner) { }

        protected DomainError(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context) { }

        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("Name", Name);
        }
    }
}

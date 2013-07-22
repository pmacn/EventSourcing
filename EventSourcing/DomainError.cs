using System;
using System.Diagnostics.Contracts;
using System.Runtime.Serialization;

namespace EventSourcing
{
    [Serializable]
    public class DomainError : Exception
    {
        public DomainError(string message) : base(message) { }

        /// <summary>
        /// Creates domain error exception with a string name that is easily identifiable in the tests
        /// </summary>
        /// <param name="name">The name to be used to identify this exception in tests.</param>
        /// <param name="format">The format.</param>
        /// <param name="args">The arguments.</param>
        /// <returns></returns>
        public static DomainError Named(string name, string format, params object[] args)
        {
            Contract.Requires<ArgumentException>(!String.IsNullOrWhiteSpace(name));

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
    }
}

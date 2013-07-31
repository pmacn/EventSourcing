using System;
using System.Diagnostics.Contracts;
using System.Linq;

namespace EventSourcing
{
    [ContractClass(typeof(IIdentityContract))]
    public interface IIdentity
    {
        /// <summary>
        /// Gets the id, converted to a string. Only alphanumerics and '-' are allowed.
        /// </summary>
        /// <returns></returns>
        string GetId();

        /// <summary>
        /// Unique tag (should be unique within the assembly) to distinguish
        /// between different identities, while deserializing.
        /// </summary>
        string GetTag();

        /// <summary>
        /// Provides consistent hashing, which will not be affected by platforms or different
        /// versions of .NET Framework
        /// </summary>
        /// <returns></returns>
        int GetConsistentHashCode();
    }

    [ContractClassFor(typeof(IIdentity))]
    internal abstract class IIdentityContract : IIdentity
    {
        [Pure]
        public string GetId()
        {
            Contract.Ensures(String.IsNullOrWhiteSpace(Contract.Result<string>()), "GetId cannot return a null, empty or whitespace string");
            throw new NotImplementedException();
        }

        [Pure]
        public string GetTag()
        {
            Contract.Ensures(String.IsNullOrWhiteSpace(Contract.Result<string>()), "GetId cannot return a null, empty or whitespace string");
            throw new NotImplementedException();
        }

        [Pure]
        public int GetConsistentHashCode()
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Base implementation of <see cref="IIdentity"/>
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    [Serializable]
    public abstract class AbstractIdentity<TKey> : IIdentity
    {
        #region Constructors

        static AbstractIdentity()
        {
            var type = typeof(TKey);
            if (type == typeof(int) || type == typeof(long) || type == typeof(uint) || type == typeof(ulong))
                return;
            if (type == typeof(Guid) || type == typeof(string))
                return;
            throw new InvalidOperationException("Abstract identity inheritors must provide stable hash. It is not supported for:  " + type);
        }

        #endregion

        #region Properties

        public abstract TKey Id { get; protected set; }

        #endregion

        #region Methods

        public string GetId()
        {
            return Id.ToString();
        }

        public abstract string GetTag();

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;

            var identity = obj as AbstractIdentity<TKey>;

            if (identity != null)
            {
                return Equals(identity);
            }

            return false;
        }

        public override string ToString()
        {
            return string.Format("{0}-{1}", GetType().Name.Replace("Id", ""), Id);
        }

        public override int GetHashCode()
        {
            return (Id.GetHashCode());
        }

        public int GetConsistentHashCode()
        {
            var type = typeof(TKey);
            return type == typeof(string) ? CalculateStringHash(Id.ToString()) : Id.GetHashCode();
        }

        private static int CalculateStringHash(string value)
        {
            if (value == null) return 42;
            unchecked
            {
                return value.Aggregate(23, (current, c) => current*31 + c);
            }
        }

        public bool Equals(AbstractIdentity<TKey> other)
        {
            if (other != null)
            {
                return other.Id.Equals(Id) && other.GetTag() == GetTag();
            }

            return false;
        }

        #endregion

        #region Operators

        [Pure]
        public static bool operator ==(AbstractIdentity<TKey> left, AbstractIdentity<TKey> right)
        {
            return Equals(left, right);
        }

        [Pure]
        public static bool operator !=(AbstractIdentity<TKey> left, AbstractIdentity<TKey> right)
        {
            return !Equals(left, right);
        }

        #endregion
    }
}

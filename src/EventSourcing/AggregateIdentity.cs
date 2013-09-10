using System;
using System.Diagnostics.Contracts;

namespace EventSourcing
{
    [ContractClass(typeof(AggregateIdentityContract))]
    public interface IAggregateIdentity
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
    }

    /// <summary>
    /// Base implementation of <see cref="IAggregateIdentity"/>
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    [Serializable]
    public abstract class AbstractAggregateIdentity<TKey> : IAggregateIdentity, IEquatable<AbstractAggregateIdentity<TKey>>
    {
        public abstract TKey Id { get; protected set; }

        public string GetId()
        {
            return Id.ToString();
        }

        public abstract string GetTag();

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;

            return Equals(obj as AbstractAggregateIdentity<TKey>);
        }

        public bool Equals(AbstractAggregateIdentity<TKey> other)
        {
            if (other == null)
                return false;

            return other.Id.Equals(Id) && other.GetTag() == GetTag();
        }

        public override string ToString()
        {
            return string.Format("{0}-{1}", GetType().Name.Replace("Id", ""), Id);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        [Pure]
        public static bool operator ==(AbstractAggregateIdentity<TKey> left, AbstractAggregateIdentity<TKey> right)
        {
            return Equals(left, right);
        }

        [Pure]
        public static bool operator !=(AbstractAggregateIdentity<TKey> left, AbstractAggregateIdentity<TKey> right)
        {
            return !Equals(left, right);
        }
    }

    [ContractClassFor(typeof(IAggregateIdentity))]
    internal abstract class AggregateIdentityContract : IAggregateIdentity
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
}

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace EventSourcing
{
    /// <summary>
    /// Abstract base class for value objects, implement GetDefiningValues
    /// to return a collection of all values that matter for instance equality
    /// </summary>
    public abstract class ValueObject : IEquatable<ValueObject>
    {
        /// <summary>
        /// Gets all the values that define the <see cref="ValueObject"/>
        /// </summary>
        /// <returns></returns>
        [Pure]
        protected abstract IEnumerable<object> GetDefiningValues();

        [Pure]
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, null)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return Equals(obj as ValueObject);
        }

        [Pure]
        public bool Equals(ValueObject other)
        {
            if(other == null)
                return false;

            var thisValuesEnumerator = GetDefiningValues().GetEnumerator();
            var thatValuesEnumerator = other.GetDefiningValues().GetEnumerator();
            while (thisValuesEnumerator.MoveNext() && thatValuesEnumerator.MoveNext())
            {
                if(!thisValuesEnumerator.Current.Equals(thatValuesEnumerator.Current))
                    return false;
            }

            return true;
        }

        [Pure]
        public override int GetHashCode()
        {
            var values = GetDefiningValues();
            unchecked
            {
                return values.Aggregate(23, (current, value) => current*31 + value.GetHashCode());
            }
        }

        [Pure]
        public static bool operator ==(ValueObject left, ValueObject right)
        {
            return Equals(left, right);
        }

        [Pure]
        public static bool operator !=(ValueObject left, ValueObject right)
        {
            return !Equals(left, right);
        }
    }
}
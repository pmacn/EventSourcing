using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace EventSourcing.Persistence
{
    [ContractClass(typeof(ConflictDetectorContract))]
    public interface IConflictDetector
    {
        /// <summary>
        /// Checks to see if there's a conflict between the committed and uncommitted events
        /// </summary>
        /// <param name="committed">The events already committed to storage.</param>
        /// <param name="uncommitted">The events not yet committed to storage</param>
        /// <returns>True if there is is a conflict detected, otherwise false.</returns>
        /// <exception cref="ArgumentNullException">committed or uncommitted is null, or any of the events in committed or uncommitted is null</exception>
        bool HasConflict(IEnumerable<IEvent> committed, IEnumerable<IEvent> uncommitted);
    }

    /// <summary>
    /// Conflict detector that registers delegates to detect conflicts.
    /// A conflict will automatically be assumed if there is no delegate
    /// for a specific pair of events.
    /// </summary>
    // TODO : need some way to indicate that a specific event does not conflict with any other event
    public class DelegateConflictDetector : IConflictDetector
    {
        private readonly ConcurrentDictionary<Type, ConcurrentDictionary<Type, Func<object, object, bool>>> _delegates =
            new ConcurrentDictionary<Type, ConcurrentDictionary<Type, Func<object, object, bool>>>();

        public void AddDelegate<TCommitted, TUncommitted>(Func<TCommitted, TUncommitted, bool> conflictDelegate)
            where TCommitted : class, IEvent
            where TUncommitted : class, IEvent
        {
            Contract.Requires<ArgumentNullException>(conflictDelegate != null, "conflictDelegate cannot be null");

            var committedType = typeof (TCommitted);
            var uncommittedType = typeof (TUncommitted);
            var d = _delegates.GetOrAdd(committedType, new ConcurrentDictionary<Type, Func<object, object, bool>>());
            Func<object, object, bool> addValue = (c, u) => conflictDelegate((TCommitted) c, (TUncommitted) u);
            d.AddOrUpdate(uncommittedType, addValue, (type, func) => addValue);
        }

        public bool HasConflict(IEnumerable<IEvent> committedEvents, IEnumerable<IEvent> uncommittedEvents)
        {
            return (from committed in committedEvents
                    from uncommitted in uncommittedEvents
                    where Conflicts(committed, uncommitted)
                    select committed).Any();
        }

        private bool Conflicts(IEvent committed, IEvent uncommitted)
        {
            var delegatesForCommittedType = _delegates.GetOrAdd(committed.GetType(),
                                                                new ConcurrentDictionary<Type, Func<object, object, bool>>());

            Func<object, object, bool> conflictDelegate;
            return !delegatesForCommittedType.TryGetValue(uncommitted.GetType(), out conflictDelegate) ||
                   conflictDelegate == null ||
                   conflictDelegate(committed, uncommitted);
        }
    }

    [ContractClassFor(typeof(IConflictDetector))]
    internal abstract class ConflictDetectorContract : IConflictDetector
    {
        public bool HasConflict(IEnumerable<IEvent> committed, IEnumerable<IEvent> uncommitted)
        {
            Contract.Requires<ArgumentNullException>(committed != null, "committed cannot be null");
            Contract.Requires<ArgumentNullException>(Contract.ForAll(committed, e => e != null), "no events in committed can be null");
            Contract.Requires<ArgumentNullException>(uncommitted != null, "uncommitted cannot be null");
            Contract.Requires<ArgumentNullException>(Contract.ForAll(uncommitted, e => e != null), "no events in uncommitted can be null");
            throw new NotImplementedException();
        }
    }
}

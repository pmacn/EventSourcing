﻿using EventSourcing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace EventStore
{
    [ContractClass(typeof(ConflictDetectorContract))]
    public interface IConflictDetector
    {
        bool HasConflict(IEnumerable<IEvent> committed, IEnumerable<IEvent> uncommitted);
    }

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

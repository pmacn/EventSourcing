using EventSourcing.Exceptions;
using System;

namespace EventSourcing
{
    public interface IAggregateFactory
    {
        TAggregate Create<TAggregate>() where TAggregate : IAggregateRoot;
    }

    public class ReflectionAggregateFactory : IAggregateFactory
    {
        public TAggregate Create<TAggregate>() where TAggregate : IAggregateRoot
        {
            var aggregateType = typeof(TAggregate);
            var ctor = aggregateType.GetConstructor(Type.EmptyTypes);
            if (ctor == null)
                throw new AggregateConstructionException(String.Format("Could not find a default constructor for {0}", aggregateType.Name));

            return (TAggregate)ctor.Invoke(new object[0]);
        }
    }
}

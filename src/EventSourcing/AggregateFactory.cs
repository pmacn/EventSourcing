using EventSourcing.Exceptions;
using System;
using System.Collections.Generic;

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

    
    public class DelegateAggregateFactory : IAggregateFactory
    {
        private readonly Dictionary<Type, Func<object>> factoryMethods = new Dictionary<Type, Func<object>>();

        public void Register<TAggregate>(Func<TAggregate> factoryMethod) where TAggregate : IAggregateRoot
        {
            var aggregateType = typeof(TAggregate);
            factoryMethods.Add(aggregateType, () => factoryMethod());
        }

        public TAggregate Create<TAggregate>()
            where TAggregate : IAggregateRoot
        {
            Func<object> factoryMethod;
            var aggregateType = typeof(TAggregate);
            if(!factoryMethods.TryGetValue(aggregateType, out factoryMethod))
                throw new AggregateConstructionException("No registered factory method for aggregate of type " + aggregateType.Name);

            return (TAggregate)factoryMethod();
        }
    }
}

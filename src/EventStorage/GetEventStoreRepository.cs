using EventSourcing;
using EventStore.ClientAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventStore
{
    //public class GetEventStoreRepository : IRepository
    //{
    //    private readonly IEventStoreConnection _connection;

    //    private readonly IConflictDetector _conflictDetector;
        
    //    public GetEventStoreRepository(IEventStoreConnection connection, IConflictDetector conflictDetector)
    //    {
    //        Contract.Requires<ArgumentNullException>(connection != null, "connection cannot be null");
    //        _connection = connection;
    //        _conflictDetector = conflictDetector;
    //    }

    //    public TAggregate GetById<TAggregate>(IIdentity aggregateId) where TAggregate : IAggregateRoot
    //    {

    //        throw new NotImplementedException();
    //    }

    //    public void Save<TIdentity>(IAggregateRoot<TIdentity> aggregate) where TIdentity : IIdentity
    //    {
    //        throw new NotImplementedException();
    //    }
    //}

    //public interface IConflictDetector
    //{
    //}
}

﻿using System.Threading.Tasks;

namespace EventSourcing.Persistence
{
    public interface IAsyncRepository
    {
        Task<TAggregate> GetByIdAsync<TAggregate>(IAggregateIdentity aggregateId)
            where TAggregate : class, IAggregateRoot;

        Task SaveAsync(IAggregateRoot aggregate);
    }
}

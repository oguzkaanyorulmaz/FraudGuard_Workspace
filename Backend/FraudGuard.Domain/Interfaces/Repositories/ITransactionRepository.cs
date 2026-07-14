using FraudGuard.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FraudGuard.Domain.Interfaces.Repositories
{
    public interface ITransactionRepository
    {
        Task AddAsync(ETransaction transaction);
        Task<List<ETransaction>> GetRecentTransactionsAsync(int cardId, TimeSpan timeWindow);
    }
}
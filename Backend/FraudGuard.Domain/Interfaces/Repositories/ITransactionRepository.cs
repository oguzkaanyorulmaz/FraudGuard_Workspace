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
        Task<bool> HasAnySuspiciousTransactionAsync(int cardId);
        Task<List<ETransaction>> GetLast10TransactionsForCardAsync(int cardId, int excludeTransactionId);
        Task<int> GetUnrefundedSaleCountAsync(int cardId, decimal amount, string currency);
    }
}
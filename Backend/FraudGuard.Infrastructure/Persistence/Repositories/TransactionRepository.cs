using FraudGuard.Domain.Entities;
using FraudGuard.Domain.Interfaces.Repositories;
using FraudGuard.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FraudGuard.Infrastructure.Persistence.Repositories
{
    public class TransactionRepository : ITransactionRepository
    {
        private readonly FraudGuardDbContext _context;

        public TransactionRepository(FraudGuardDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(ETransaction transaction)
        {
            await _context.Transactions.AddAsync(transaction);
        }

        public async Task<List<ETransaction>> GetRecentTransactionsAsync(int cardId, TimeSpan timeWindow)
        {
            var cutoffTime = DateTime.Now.Subtract(timeWindow);
            
            return await _context.Transactions
                .Where(t => t.CardId == cardId && t.TransactionDate >= cutoffTime)
                .ToListAsync();
        }
    }
}
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
                .Where(t => (t.CreditCardId == cardId || t.DebitCardId == cardId) && t.TransactionDate >= cutoffTime)
                .ToListAsync();
        }
        public async Task<bool> HasAnySuspiciousTransactionAsync(int cardId, bool isCreditCard)
        {
            if (isCreditCard)
            {
                return await _context.Transactions.AnyAsync(t => t.CreditCardId == cardId && t.Status == "Suspicious");
            }
            else
            {
                return await _context.Transactions.AnyAsync(t => t.DebitCardId == cardId && t.Status == "Suspicious");
            }
        }
        public async Task<List<ETransaction>> GetLast10TransactionsForCardAsync(int cardId, int excludeTransactionId)
        {
            return await _context.Transactions
                .Include(t => t.TransactionType)
                .Include(t => t.FraudLog)
                    .ThenInclude(fl => fl.FraudRule)
                .Where(t => (t.CreditCardId == cardId || t.DebitCardId == cardId) && t.TransactionId != excludeTransactionId)
                .OrderByDescending(t => t.TransactionDate)
                .Take(10)
                .ToListAsync();
        }

        public async Task<int> GetUnrefundedSaleCountAsync(int cardId, decimal amount, string currency)
        {
            var approvedSalesCount = await _context.Transactions.CountAsync(t => 
                (t.CreditCardId == cardId || t.DebitCardId == cardId) && 
                t.TransactionTypeId == 1 && // 1: Sale
                t.Amount == amount && 
                t.Currency == currency && 
                t.Status == "Approved");

            var refundsCount = await _context.Transactions.CountAsync(t => 
                (t.CreditCardId == cardId || t.DebitCardId == cardId) && 
                t.TransactionTypeId == 2 && // 2: Refund
                t.Amount == amount && 
                t.Currency == currency && 
                (t.Status == "Approved" || t.Status == "Suspicious"));

            return approvedSalesCount - refundsCount;
        }

        public async Task<List<ETransaction>> GetRecentTransactionsByReceiverIBANAsync(string receiverIBAN, TimeSpan timeWindow)
        {
            var cutoffTime = DateTime.Now.Subtract(timeWindow);
            return await _context.Transactions
                .Where(t => t.ReceiverIBAN == receiverIBAN && t.TransactionDate >= cutoffTime)
                .ToListAsync();
        }
    }
}
using FraudGuard.Domain.Entities;
using FraudGuard.Domain.Interfaces.Entities;
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

        public async Task AddCreditCardTransactionAsync(ECreditCardTransaction transaction)
        {
            await _context.CreditCardTransactions.AddAsync(transaction);
        }

        public async Task AddDebitCardTransactionAsync(EDebitCardTransaction transaction)
        {
            await _context.DebitCardTransactions.AddAsync(transaction);
        }

        public async Task AddTransferTransactionAsync(ETransferTransaction transaction)
        {
            await _context.TransferTransactions.AddAsync(transaction);
        }

        public async Task<List<ITransaction>> GetRecentTransactionsAsync(int cardId, bool isCreditCard, TimeSpan timeWindow)
        {
            var cutoffTime = DateTime.Now.Subtract(timeWindow);

            if (isCreditCard)
            {
                var txs = await _context.CreditCardTransactions
                    .Where(t => t.CreditCardId == cardId && t.TransactionDate >= cutoffTime)
                    .ToListAsync();
                return txs.Cast<ITransaction>().ToList();
            }
            else
            {
                var txs = await _context.DebitCardTransactions
                    .Where(t => t.DebitCardId == cardId && t.TransactionDate >= cutoffTime)
                    .ToListAsync();
                return txs.Cast<ITransaction>().ToList();
            }
        }

        public async Task<List<ITransaction>> GetRecentTransactionsByMerchantAsync(
            string merchantId, TimeSpan timeWindow)
        {
            if (string.IsNullOrWhiteSpace(merchantId))
                return new List<ITransaction>();

            var cutoffTime = DateTime.Now.Subtract(timeWindow);

            // İki tablo ayrı sorgulanır: farklı tiplerdir, EF tarafında birleştirilemezler.
            var creditTxs = await _context.CreditCardTransactions
                .AsNoTracking()
                .Where(t => t.MerchantId == merchantId && t.TransactionDate >= cutoffTime)
                .ToListAsync();

            var debitTxs = await _context.DebitCardTransactions
                .AsNoTracking()
                .Where(t => t.MerchantId == merchantId && t.TransactionDate >= cutoffTime)
                .ToListAsync();

            return creditTxs.Cast<ITransaction>()
                .Concat(debitTxs.Cast<ITransaction>())
                .ToList();
        }

        public async Task<bool> HasAnySuspiciousTransactionAsync(int cardId, bool isCreditCard)
        {
            if (isCreditCard)
            {
                return await _context.CreditCardTransactions.AnyAsync(t => 
                    t.CreditCardId == cardId && 
                    (t.Status == "Suspicious" || t.Status == "SuspiciousRefund" || 
                     (t.FraudLog != null && !t.FraudLog.IsResolved)));
            }
            else
            {
                return await _context.DebitCardTransactions.AnyAsync(t => 
                    t.DebitCardId == cardId && 
                    (t.Status == "Suspicious" || t.Status == "SuspiciousRefund" || 
                     (t.FraudLog != null && !t.FraudLog.IsResolved)));
            }
        }

        public async Task<List<ITransaction>> GetLast10TransactionsForCardAsync(int cardId, bool isCreditCard, DateTime beforeDate)
        {
            if (isCreditCard)
            {
                var txs = await _context.CreditCardTransactions
                    .Include(t => t.TransactionType)
                    .Include(t => t.FraudLog)
                        .ThenInclude(fl => fl.FraudRule)
                    .Where(t => t.CreditCardId == cardId && t.TransactionDate < beforeDate)
                    .OrderByDescending(t => t.TransactionDate)
                    .Take(10)
                    .ToListAsync();
                return txs.Cast<ITransaction>().ToList();
            }
            else
            {
                var txs = await _context.DebitCardTransactions
                    .Include(t => t.TransactionType)
                    .Include(t => t.FraudLog)
                        .ThenInclude(fl => fl.FraudRule)
                    .Where(t => t.DebitCardId == cardId && t.TransactionDate < beforeDate)
                    .OrderByDescending(t => t.TransactionDate)
                    .Take(10)
                    .ToListAsync();
                return txs.Cast<ITransaction>().ToList();
            }
        }

        public async Task<List<ITransaction>> GetLast10SuspiciousTransactionsForCardAsync(int cardId, bool isCreditCard, DateTime beforeDate)
        {
            if (isCreditCard)
            {
                var txs = await _context.CreditCardTransactions
                    .Include(t => t.TransactionType)
                    .Include(t => t.FraudLog)
                        .ThenInclude(fl => fl.FraudRule)
                    .Where(t => t.CreditCardId == cardId && t.TransactionDate < beforeDate && (t.Status == "Suspicious" || t.Status == "SuspiciousRefund" || t.FraudLog != null))
                    .OrderByDescending(t => t.TransactionDate)
                    .Take(10)
                    .ToListAsync();
                return txs.Cast<ITransaction>().ToList();
            }
            else
            {
                var txs = await _context.DebitCardTransactions
                    .Include(t => t.TransactionType)
                    .Include(t => t.FraudLog)
                        .ThenInclude(fl => fl.FraudRule)
                    .Where(t => t.DebitCardId == cardId && t.TransactionDate < beforeDate && (t.Status == "Suspicious" || t.Status == "SuspiciousRefund" || t.FraudLog != null))
                    .OrderByDescending(t => t.TransactionDate)
                    .Take(10)
                    .ToListAsync();
                return txs.Cast<ITransaction>().ToList();
            }
        }

        public async Task<bool> HasBeenRefundedAsync(string rrn, int cardId, bool isCreditCard)
        {
            if (isCreditCard)
            {
                return await _context.CreditCardTransactions.AnyAsync(t => 
                    t.CreditCardId == cardId && 
                    t.RRN == rrn && 
                    t.TransactionTypeId == 2 && // 2: Refund
                    (t.Status == "Approved" || t.Status == "Suspicious" || t.Status == "SuspiciousRefund"));
            }
            else
            {
                return await _context.DebitCardTransactions.AnyAsync(t => 
                    t.DebitCardId == cardId && 
                    t.RRN == rrn && 
                    t.TransactionTypeId == 2 && // 2: Refund
                    (t.Status == "Approved" || t.Status == "Suspicious" || t.Status == "SuspiciousRefund"));
            }
        }

        public async Task<ITransaction?> GetOriginalSaleByRrnAsync(string rrn, int cardId, bool isCreditCard)
        {
            if (isCreditCard)
            {
                var tx = await _context.CreditCardTransactions
                    .FirstOrDefaultAsync(t => t.CreditCardId == cardId && t.RRN == rrn && t.TransactionTypeId == 1 && t.Status == "Approved");
                return tx;
            }
            else
            {
                var tx = await _context.DebitCardTransactions
                    .FirstOrDefaultAsync(t => t.DebitCardId == cardId && t.RRN == rrn && t.TransactionTypeId == 1 && t.Status == "Approved");
                return tx;
            }
        }

        public async Task<List<ETransferTransaction>> GetRecentTransferTransactionsByReceiverIBANAsync(string receiverIBAN, TimeSpan timeWindow)
        {
            var cutoffTime = DateTime.Now.Subtract(timeWindow);
            return await _context.TransferTransactions
                .Where(t => t.ReceiverIBAN == receiverIBAN && t.TransactionDate >= cutoffTime)
                .ToListAsync();
        }

        public async Task<List<ETransferTransaction>> GetRecentTransferTransactionsBySenderIBANAsync(string senderIBAN, TimeSpan timeWindow)
        {
            var cutoffTime = DateTime.Now.Subtract(timeWindow);
            return await _context.TransferTransactions
                .Where(t => t.SenderIBAN == senderIBAN && t.TransactionDate >= cutoffTime)
                .ToListAsync();
        }

        public async Task<List<ETransferTransaction>> GetLast10SentTransfersForIBANAsync(string iban, DateTime beforeDate)
        {
            return await _context.TransferTransactions
                .Include(t => t.FraudLog)
                    .ThenInclude(fl => fl.FraudRule)
                .Where(t => t.SenderIBAN == iban && t.TransactionDate < beforeDate)
                .OrderByDescending(t => t.TransactionDate)
                .Take(10)
                .ToListAsync();
        }

        public async Task<List<ETransferTransaction>> GetLast10ReceivedTransfersForIBANAsync(string iban, DateTime beforeDate)
        {
            return await _context.TransferTransactions
                .Include(t => t.FraudLog)
                    .ThenInclude(fl => fl.FraudRule)
                .Where(t => t.ReceiverIBAN == iban && t.TransactionDate < beforeDate)
                .OrderByDescending(t => t.TransactionDate)
                .Take(10)
                .ToListAsync();
        }

        public async Task<ECreditCardTransaction?> GetCreditCardByIdAsync(int id)
        {
            return await _context.CreditCardTransactions
                .Include(t => t.CreditCard)
                .FirstOrDefaultAsync(t => t.TransactionId == id);
        }

        public async Task<EDebitCardTransaction?> GetDebitCardByIdAsync(int id)
        {
            return await _context.DebitCardTransactions
                .Include(t => t.DebitCard)
                .FirstOrDefaultAsync(t => t.TransactionId == id);
        }

        public async Task<ETransferTransaction?> GetTransferByIdAsync(int id)
        {
            return await _context.TransferTransactions
                .FirstOrDefaultAsync(t => t.TransactionId == id);
        }
    }
}
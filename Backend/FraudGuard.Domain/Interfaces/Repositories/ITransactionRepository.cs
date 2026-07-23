using FraudGuard.Domain.Entities;
using FraudGuard.Domain.Interfaces.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FraudGuard.Domain.Interfaces.Repositories
{
    public interface ITransactionRepository
    {
        // Kayıt ekleme metotları
        Task AddCreditCardTransactionAsync(ECreditCardTransaction transaction);
        Task AddDebitCardTransactionAsync(EDebitCardTransaction transaction);
        Task AddTransferTransactionAsync(ETransferTransaction transaction);

        // Kural kontrolleri ve geçmiş sorguları
        Task<List<ITransaction>> GetRecentTransactionsAsync(int cardId, bool isCreditCard, TimeSpan timeWindow);
        Task<bool> HasAnySuspiciousTransactionAsync(int cardId, bool isCreditCard);
        Task<List<ITransaction>> GetLast10TransactionsForCardAsync(int cardId, bool isCreditCard, DateTime beforeDate);
        Task<List<ITransaction>> GetLast10SuspiciousTransactionsForCardAsync(int cardId, bool isCreditCard, DateTime beforeDate);
        
        // RRN bazlı mükerrer iade (Refund) engelleme kontrolü
        Task<bool> HasBeenRefundedAsync(string rrn, int cardId, bool isCreditCard);
        Task<ITransaction?> GetOriginalSaleByRrnAsync(string rrn, int cardId, bool isCreditCard);
        
        // Transfer bazlı sorgular
        Task<List<ETransferTransaction>> GetRecentTransferTransactionsByReceiverIBANAsync(string receiverIBAN, TimeSpan timeWindow);
        Task<List<ETransferTransaction>> GetRecentTransferTransactionsBySenderIBANAsync(string senderIBAN, TimeSpan timeWindow);
        Task<List<ETransferTransaction>> GetLast10SentTransfersForIBANAsync(string iban, DateTime beforeDate);
        Task<List<ETransferTransaction>> GetLast10ReceivedTransfersForIBANAsync(string iban, DateTime beforeDate);

        // Tekil sorgulamalar
        Task<ECreditCardTransaction?> GetCreditCardByIdAsync(int id);
        Task<EDebitCardTransaction?> GetDebitCardByIdAsync(int id);
        Task<ETransferTransaction?> GetTransferByIdAsync(int id);
    }
}
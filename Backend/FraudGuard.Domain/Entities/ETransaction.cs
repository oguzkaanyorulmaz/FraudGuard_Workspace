using System;
using FraudGuard.Domain.Common.Enums;

namespace FraudGuard.Domain.Entities
{
    public class ETransaction
    {
        public int TransactionId { get; set; }
        public int CardId { get; set; }
        
        public int TransactionTypeId { get; set; } 
        public int PaymentTypeId { get; set; }
        public string Currency { get; set; } = "TRY";
        
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; }
        public string Location { get; set; }
        public string Country { get; set; }
        public string MerchantCategory { get; set; }
        public string Status { get; set; }
        public string? DeclineReason { get; set; }
        public string? FraudReason { get; set; }
        
// Mevcut propertilerin arasına ekle
        public ECreditCard CreditCard { get; set; }
        public ETransactionType TransactionType { get; set; }
        public EPaymentType PaymentType { get; set; }
        public EFraudLog FraudLog { get; set; }
    }
}
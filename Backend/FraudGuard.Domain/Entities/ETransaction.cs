using System;
using FraudGuard.Domain.Common.Enums;

namespace FraudGuard.Domain.Entities
{
    public class ETransaction
    {
        public int TransactionId { get; set; }
        

        public int? CreditCardId { get; set; }
        public int? DebitCardId { get; set; }
        
        public string? SenderIBAN { get; set; }
        public string? ReceiverIBAN { get; set; }
        public string? ReceiverName { get; set; }
        public string? Description { get; set; }

        public int TransactionTypeId { get; set; } 
        public PaymentTypeEnum PaymentType { get; set; }
        public int ChannelTypeId { get; set; } 
        public string Currency { get; set; } = "TRY";
        
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; }
        public string Location { get; set; }
        public string Country { get; set; }
        public string MerchantCategory { get; set; }
        public string Status { get; set; }
        public string? DeclineReason { get; set; }
        public string? FraudReason { get; set; }
        
        public virtual ECreditCard? CreditCard { get; set; }
        public virtual EDebitCard? DebitCard { get; set; }
        public virtual ETransactionType TransactionType { get; set; }
        public virtual EChannelType ChannelType { get; set; }
        public virtual EFraudLog FraudLog { get; set; }

        public int? OriginalTransactionId { get; set; }
        public virtual ETransaction? OriginalTransaction { get; set; }
        public DateTime? RefundTime { get; set; }
    }
}

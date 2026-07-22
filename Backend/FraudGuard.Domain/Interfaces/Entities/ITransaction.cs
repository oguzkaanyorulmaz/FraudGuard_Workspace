using System;
using FraudGuard.Domain.Common.Enums;
using FraudGuard.Domain.Entities;

namespace FraudGuard.Domain.Interfaces.Entities
{
    public interface ITransaction
    {
        int TransactionId { get; }
        string RRN { get; }
        int? CreditCardId { get; }
        int? DebitCardId { get; }
        string? SenderIBAN { get; }
        string? ReceiverIBAN { get; }
        string? ReceiverName { get; }
        string? Description { get; }
        
        int TransactionTypeId { get; } 
        PaymentTypeEnum PaymentType { get; }
        int ChannelTypeId { get; }
        string Currency { get; }
        decimal Amount { get; }
        DateTime TransactionDate { get; }
        string Location { get; }
        string Country { get; }
        string? MerchantCategory { get; }
        string Status { get; set; }
        string? DeclineReason { get; set; }
        string? FraudReason { get; set; }
        
        EFraudLog? FraudLog { get; }
    }
}

using System;
using FraudGuard.Domain.Common.Enums;
using FraudGuard.Domain.Interfaces.Entities;

namespace FraudGuard.Domain.Entities
{
    public class ETransferTransaction : ITransaction
    {
        public int TransactionId { get; set; }
        public string RRN { get; set; } // 12 Haneli Referans Numarası
        
        public string SenderIBAN { get; set; }
        public string ReceiverIBAN { get; set; }
        public string ReceiverName { get; set; }
        public string? Description { get; set; }
        
        public int ChannelTypeId { get; set; } // 4: Mobile, 5: Web, vb.
        public string Currency { get; set; } = "TRY";
        
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; }
        public string Location { get; set; }
        public string Country { get; set; }
        public string Status { get; set; }
        public string? DeclineReason { get; set; }
        public string? FraudReason { get; set; }
        
        // Navigation Properties
        public virtual EChannelType ChannelType { get; set; }
        public virtual EFraudLog? FraudLog { get; set; }

        // ITransaction Implementations
        public int? CreditCardId => null;
        public int? DebitCardId => null;
        public int TransactionTypeId => 4; // Transfer
        public PaymentTypeEnum PaymentType => PaymentTypeEnum.EFT; // Ya da BankTransfer
        public string? MerchantCategory => "Transfer";
    }
}

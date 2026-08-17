using System;
using FraudGuard.Domain.Common.Enums;
using FraudGuard.Domain.Interfaces.Entities;

namespace FraudGuard.Domain.Entities
{
    public class EDebitCardTransaction : ITransaction
    {
        public int TransactionId { get; set; }
        public string RRN { get; set; } // 12 Haneli Referans Numarası
        
        public int DebitCardId { get; set; }
        public int TransactionTypeId { get; set; } // 1: Sale, 2: Refund
        public int ChannelTypeId { get; set; } // 1: POS, 2: VirtualPOS, 3: ATM, vb.
        public string Currency { get; set; } = "TRY";
        
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; }
        public string Location { get; set; }
        public string Country { get; set; }
        public string MerchantCategory { get; set; }
        public string Status { get; set; }
        public string? DeclineReason { get; set; }
        public string? FraudReason { get; set; }

        /// <summary>Fraud motorunun nihai skoru. Karar anında yazılır.</summary>
        public int RiskScore { get; set; }

        /// <summary>Skorun karşılık geldiği kademe.</summary>
        public RiskDecisionEnum RiskDecision { get; set; } = RiskDecisionEnum.Normal;

        // Navigation Properties
        public virtual EDebitCard DebitCard { get; set; }
        public virtual ETransactionType TransactionType { get; set; }
        public virtual EChannelType ChannelType { get; set; }
        public virtual EFraudLog? FraudLog { get; set; }

        // ITransaction Implementations
        public int? CreditCardId => null;
        int? ITransaction.DebitCardId => DebitCardId;
        public string? SenderIBAN => null;
        public string? ReceiverIBAN => null;
        public string? ReceiverName => null;
        public string? Description => null;
        public PaymentTypeEnum PaymentType => PaymentTypeEnum.DebitCard;
    }
}

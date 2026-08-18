using System;
using FraudGuard.Domain.Common.Enums;
using FraudGuard.Domain.Interfaces.Entities;

namespace FraudGuard.Domain.Entities
{
    public class ECreditCardTransaction : ITransaction
    {
        public int TransactionId { get; set; }
        public string RRN { get; set; } // 12 Haneli Referans Numarası
        
        public int CreditCardId { get; set; }
        public int TransactionTypeId { get; set; } // 1: Sale, 2: Refund
        public int ChannelTypeId { get; set; } // 1: POS, 2: VirtualPOS, vb.
        public string Currency { get; set; } = "TRY";
        
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; }
        public string Location { get; set; }
        public string Country { get; set; }
        public string MerchantCategory { get; set; }

        /// <summary>İşlemin geçtiği üye işyeri. İşyeri seçilmeden gönderilen işlemlerde null.</summary>
        public string? MerchantId { get; set; }

        public string Status { get; set; }
        public string? DeclineReason { get; set; }
        public string? FraudReason { get; set; }

        /// <summary>Fraud motorunun nihai skoru. Karar anında yazılır.</summary>
        public int RiskScore { get; set; }

        /// <summary>Skorun karşılık geldiği kademe.</summary>
        public RiskDecisionEnum RiskDecision { get; set; } = RiskDecisionEnum.Normal;

        // Navigation Properties
        public virtual ECreditCard CreditCard { get; set; }
        public virtual ETransactionType TransactionType { get; set; }
        public virtual EChannelType ChannelType { get; set; }
        public virtual EFraudLog? FraudLog { get; set; }

        // ITransaction Implementations
        int? ITransaction.CreditCardId => CreditCardId;
        public int? DebitCardId => null;
        public string? SenderIBAN => null;
        public string? ReceiverIBAN => null;
        public string? ReceiverName => null;
        public string? Description => null;
        public PaymentTypeEnum PaymentType => PaymentTypeEnum.CreditCard;
    }
}

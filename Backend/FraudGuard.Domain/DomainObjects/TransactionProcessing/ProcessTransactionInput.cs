using System;
using FraudGuard.Domain.Common.Enums;

namespace FraudGuard.Domain.DomainObjects.TransactionProcessing
{
    public class ProcessTransactionInput
    {
        public string CardNumber { get; set; }
        public decimal Amount { get; set; }
        
        public string Currency { get; set; } = "TRY"; 
        public TransactionTypeEnum TransactionType { get; set; } 
        public PaymentTypeEnum PaymentType { get; set; }
        public string CVV { get; set; }
        public string Location { get; set; }
        public string Country { get; set; } = "Türkiye";
        public string MerchantCategory { get; set; }
    }
}
using FraudGuard.Application.DTOs;
using FraudGuard.Domain.Common.Enums;

namespace FraudGuard.Application.DTOs.TransactionProcessing
{
    public class ProcessTransactionRequest : RequestDTO 
    {
        public string CardNumber { get; set; }
        public string ExpiryDate { get; set; }
        public string CVV { get; set; }

        public decimal Amount { get; set; }
        public string Currency { get; set; } = "TRY"; 
        public TransactionTypeEnum TransactionType { get; set; }
        public PaymentTypeEnum PaymentType { get; set; }
        public string Location { get; set; }
        public string Country { get; set; } = "Türkiye";
        public string MerchantCategory { get; set; }
    }
}
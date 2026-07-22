using FraudGuard.Domain.Common.Enums;

namespace FraudGuard.Domain.DomainObjects.TransactionProcessing
{
    public class ProcessTransactionInput
    {
        public string? CardNumber { get; set; }
        public string? ExpiryDate { get; set; }
        public string? CVV { get; set; }

        public string? SenderIBAN { get; set; }
        public string? ReceiverIBAN { get; set; }
        public string? ReceiverName { get; set; }
        public string? Description { get; set; }

        public decimal Amount { get; set; }
        public string Currency { get; set; } = "TRY";
        public TransactionTypeEnum TransactionType { get; set; }
        public PaymentTypeEnum PaymentType { get; set; }
        public int ChannelTypeId { get; set; }

        public string Location { get; set; }
        public string Country { get; set; } = "Türkiye";
        public string MerchantCategory { get; set; } = "Diğer";
        public int? OriginalTransactionId { get; set; }
        public string? RRN { get; set; }
    }
}

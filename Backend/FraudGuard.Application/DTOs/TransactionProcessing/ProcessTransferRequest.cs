namespace FraudGuard.Application.DTOs.TransactionProcessing
{
    public class ProcessTransferRequest
    {
        public string SenderIBAN { get; set; }
        public string ReceiverIBAN { get; set; }
        public string ReceiverName { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "TRY";
        public string Country { get; set; } = "Türkiye";
    }
}

namespace FraudGuard.Application.DTOs.TransactionProcessing
{
    public class ProcessTransactionResponse
    {
        public int? TransactionId { get; set; } 
        public string Status { get; set; }
        public string DeclineReason { get; set; }
        public string RRN { get; set; }
    }
}
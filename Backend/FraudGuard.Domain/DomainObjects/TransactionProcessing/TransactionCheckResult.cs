namespace FraudGuard.Domain.DomainObjects.TransactionProcessing
{
    public class TransactionCheckResult
    {
        public int? TransactionId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string DeclineReason { get; set; } = string.Empty;

        public bool IsSuspicious { get; set; }
        public int? TriggeredRuleId { get; set; }
        public string TriggeredRuleName { get; set; } = string.Empty;
        public string RRN { get; set; } = string.Empty;
    }
}
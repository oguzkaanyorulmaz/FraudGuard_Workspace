using System;

namespace FraudGuard.Application.DTOs.FraudManagement
{
    public class GetUnresolvedLogsResponse
    {
        public int LogId { get; set; }
        public int TransactionId { get; set; }
        public string? RuleName { get; set; }
        public string? SuspicionReason { get; set; }
        public int RiskScore { get; set; }        
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string MaskedCardNumber { get; set; }
        public DateTime LogDate { get; set; }
        public DateTime TransactionDate { get; set; }
        public string? AdminAction { get; set; }
    }
}
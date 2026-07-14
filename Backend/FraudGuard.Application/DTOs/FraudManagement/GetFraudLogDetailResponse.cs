using System;

namespace FraudGuard.Application.DTOs.FraudManagement
{
    public class GetFraudLogDetailResponse
    {
        public int LogId { get; set; }
        public int TransactionId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public DateTime TransactionDate { get; set; }
        public string Location { get; set; }
        public string Country { get; set; }
        
        public string RuleName { get; set; } 
        public string SuspicionReason { get; set; }
        public string FraudReason { get; set; }

        public string MaskedCardNumber { get; set; }
        public decimal CardLimit { get; set; }
        public bool IsCardBlocked { get; set; }

        public string CustomerFullName { get; set; }
        public string IdentityNumber { get; set; }
    }
}
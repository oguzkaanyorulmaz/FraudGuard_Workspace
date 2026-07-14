using System;
using System.Collections.Generic;

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
        public decimal AvailableLimit { get; set; }
        public bool IsCardBlocked { get; set; }

        public string CustomerFullName { get; set; }
        public string IdentityNumber { get; set; }
        public string? PhoneNumber { get; set; }
        public List<CardRecentTransactionDto> RecentTransactions { get; set; } = new();
        public string? AdminNote { get; set; }
        public string? ResolvedByAdmin { get; set; }
    }

    public class CardRecentTransactionDto
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string Location { get; set; }
        public string Country { get; set; }
        public DateTime TransactionDate { get; set; }
        public string MerchantCategory { get; set; }
        public string Status { get; set; }
        public string? FraudSuspicionReason { get; set; }
        public string? AdminNote { get; set; }
        public string? ResolvedByAdmin { get; set; }
    }
}

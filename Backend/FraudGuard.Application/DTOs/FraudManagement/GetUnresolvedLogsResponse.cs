using System;

namespace FraudGuard.Application.DTOs.FraudManagement
{
    public class GetUnresolvedLogsResponse
    {
        public int LogId { get; set; }
        public int TransactionId { get; set; }
        public string? RuleName { get; set; }
        public string? RuleCode { get; set; }
        public string? SuspicionReason { get; set; }
        /// <summary>Fraud motorunun işlem anında hesaplayıp kaydettiği kümülatif skor.</summary>
        public int RiskScore { get; set; }

        /// <summary>Skorun kademesi: NORMAL / IZLE / EK_DOGRULAMA / RET_BLOKE.</summary>
        public string RiskDecision { get; set; } = "NORMAL";

        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string MaskedCardNumber { get; set; }
        public DateTime LogDate { get; set; }
        public DateTime TransactionDate { get; set; }
        public string? AdminAction { get; set; }
        public string? PaymentTypeCode { get; set; }

    }
}
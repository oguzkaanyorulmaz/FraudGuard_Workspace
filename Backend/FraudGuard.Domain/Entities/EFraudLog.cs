using System;

namespace FraudGuard.Domain.Entities
{
    public class EFraudLog
    {
        public int LogId { get; set; }
        public int TransactionId { get; set; }
        public int RuleId { get; set; }
        public DateTime LogDate { get; set; } = DateTime.Now;
        public bool IsResolved { get; set; } = false;
        public string? AdminAction { get; set; }
        public string? Status { get; set; } = "Unresolved";
        public string? ResolvedByAdmin { get; set; }
        public virtual ETransaction Transaction { get; set; }
        public virtual EFraudRule FraudRule { get; set; }
        public string? AdminNote { get; set; }
    }
}
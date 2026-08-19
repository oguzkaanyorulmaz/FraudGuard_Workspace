using FraudGuard.Domain.Common.Constants;
using System;

namespace FraudGuard.Domain.Entities
{
    public class EFraudLog
    {
        public int LogId { get; set; }
        public int RuleId { get; set; }
        public DateTime LogDate { get; set; } = DateTime.Now;
        public bool IsResolved { get; set; } = false;
        public string? AdminAction { get; set; }
        public string? Status { get; set; } = FraudLogStatuses.Unresolved;
        public string? ResolvedByAdmin { get; set; }
        public string? AdminNote { get; set; }
        
        // Üç bağımsız tablo için Nullable Foreign Key'ler
        public int? CreditCardTransactionId { get; set; }
        public int? DebitCardTransactionId { get; set; }
        public int? TransferTransactionId { get; set; }
        
        // Navigation Properties
        public virtual ECreditCardTransaction? CreditCardTransaction { get; set; }
        public virtual EDebitCardTransaction? DebitCardTransaction { get; set; }
        public virtual ETransferTransaction? TransferTransaction { get; set; }
        public virtual EFraudRule FraudRule { get; set; }

        // Helper helper to return the active transaction polymorphic
        public virtual FraudGuard.Domain.Interfaces.Entities.ITransaction? Transaction
        {
            get
            {
                if (CreditCardTransaction != null) return CreditCardTransaction;
                if (DebitCardTransaction != null) return DebitCardTransaction;
                if (TransferTransaction != null) return TransferTransaction;
                return null;
            }
        }
    }
}

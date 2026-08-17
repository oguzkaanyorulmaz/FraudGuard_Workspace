using System.Collections.Generic;

namespace FraudGuard.Domain.Entities
{
    public class ETransactionType
    {
        public int TransactionTypeId { get; set; }
        public string TypeCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public virtual ICollection<ECreditCardTransaction>? CreditCardTransactions { get; set; } = new List<ECreditCardTransaction>();
        public virtual ICollection<EDebitCardTransaction>? DebitCardTransactions { get; set; } = new List<EDebitCardTransaction>();
    }
}
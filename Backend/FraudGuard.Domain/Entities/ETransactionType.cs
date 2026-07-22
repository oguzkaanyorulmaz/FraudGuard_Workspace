using System.Collections.Generic;

namespace FraudGuard.Domain.Entities
{
    public class ETransactionType
    {
        public int TransactionTypeId { get; set; }
        public string TypeCode { get; set; }
        public string Description { get; set; }
        public virtual ICollection<ECreditCardTransaction> CreditCardTransactions { get; set; }
        public virtual ICollection<EDebitCardTransaction> DebitCardTransactions { get; set; }
    }
}
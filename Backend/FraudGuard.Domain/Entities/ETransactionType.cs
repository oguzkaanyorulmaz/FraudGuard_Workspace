using System.Collections.Generic;

namespace FraudGuard.Domain.Entities
{
    public class ETransactionType
    {
        public int TransactionTypeId { get; set; }
        public string TypeCode { get; set; }
        public string Description { get; set; }
        public ICollection<ETransaction> Transactions { get; set; }
    }
}
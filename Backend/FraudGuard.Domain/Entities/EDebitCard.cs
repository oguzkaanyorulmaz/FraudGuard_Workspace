using System.Collections.Generic;

namespace FraudGuard.Domain.Entities
{
    public class EDebitCard
    {
        public int CardId { get; set; }
        public int CustomerId { get; set; }
        public string CardNumber { get; set; }
        public string ExpiryDate { get; set; }
        public string CVV { get; set; }
        public decimal Balance { get; set; } 
        public string IBAN { get; set; }
        public bool IsBlocked { get; set; } = false;
        public int? BlockReasonId { get; set; } 

        public virtual ECustomer Customer { get; set; }
        public virtual EBlockReason? BlockReason { get; set; }
        public virtual ICollection<EDebitCardTransaction> Transactions { get; set; }
    }
}

using System;

namespace FraudGuard.Domain.Entities
{
    public class EBankAccountBeneficiary
    {
        public int BeneficiaryId { get; set; }
        public int CustomerId { get; set; }
        public string ReceiverIBAN { get; set; }
        public string ReceiverName { get; set; }
        public DateTime AddedDate { get; set; } = DateTime.Now;

        public virtual ECustomer Customer { get; set; }
    }
}

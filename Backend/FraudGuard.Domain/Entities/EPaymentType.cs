using System.Collections.Generic;

namespace FraudGuard.Domain.Entities
{
    public class EPaymentType
    {
        public int PaymentTypeId { get; set; }
        public string TypeCode { get; set; }
        public string Description { get; set; }
        public ICollection<ETransaction> Transactions { get; set; }
    }
}

using System.Collections.Generic;

namespace FraudGuard.Domain.Entities
{
    public class EChannelType
    {
        public int ChannelTypeId { get; set; }
        public string ChannelCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public virtual ICollection<ECreditCardTransaction>? CreditCardTransactions { get; set; } = new List<ECreditCardTransaction>();
        public virtual ICollection<EDebitCardTransaction>? DebitCardTransactions { get; set; } = new List<EDebitCardTransaction>();
        public virtual ICollection<ETransferTransaction>? TransferTransactions { get; set; } = new List<ETransferTransaction>();
    }
}

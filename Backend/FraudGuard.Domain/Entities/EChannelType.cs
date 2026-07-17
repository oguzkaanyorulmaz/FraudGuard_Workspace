using System.Collections.Generic;

namespace FraudGuard.Domain.Entities
{
    public class EChannelType
    {
        public int ChannelTypeId { get; set; }
        public string ChannelCode { get; set; }
        public string Description { get; set; }

        public virtual ICollection<ETransaction> Transactions { get; set; }
    }
}

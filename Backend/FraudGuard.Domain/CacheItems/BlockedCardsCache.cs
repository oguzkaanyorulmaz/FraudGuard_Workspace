using System.Collections.Generic;

namespace FraudGuard.Domain.CacheItems
{
    public class BlockedCardsCache
    {
        public HashSet<string> BlockedCardNumbers { get; set; } = new HashSet<string>();
    }
}
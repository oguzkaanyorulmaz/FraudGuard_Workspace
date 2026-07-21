namespace FraudGuard.Domain.DomainObjects
{
    public class CardCacheInfo
    {
        public decimal AvailableFunds { get; set; }
        public bool IsBlocked { get; set; }
        public string CVV { get; set; } = string.Empty;
    }
}

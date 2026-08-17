namespace FraudGuard.Domain.Entities
{
    public class EBlockReason
    {
        public int ReasonId { get; set; }
        public string ReasonCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
namespace FraudGuard.Domain.Entities
{
    public class EFraudRule
    {
        public int RuleId { get; set; }
        public string RuleCode { get; set; }
        public string RuleName { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
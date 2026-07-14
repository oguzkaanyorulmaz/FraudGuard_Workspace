namespace FraudGuard.Application.DTOs.RuleManagement
{
    public class GetActiveRulesResponse
    {
        public int RuleId { get; set; }
        public string RuleCode { get; set; }
        public string RuleName { get; set; }
        public string Description { get; set; }
    }
}
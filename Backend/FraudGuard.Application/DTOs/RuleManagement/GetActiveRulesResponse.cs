namespace FraudGuard.Application.DTOs.RuleManagement
{
    public class GetActiveRulesResponse
    {
        public int RuleId { get; set; }
        public string RuleCode { get; set; } = string.Empty;
        public string RuleName { get; set; } = string.Empty;
        public string? Description { get; set; }

        /// <summary>Dinamik kuralın ifadesi. Kod tabanlı kurallarda null.</summary>
        public string? Expression { get; set; }

        /// <summary>Kural dinamik ifadeyle mi çalışıyor, kod tabanlı mı.</summary>
        public bool IsExpressionBased { get; set; }

        public int Score { get; set; }
        public string Target { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        /// <summary>Puanı güven indiriminden muaf mı.</summary>
        public bool IsCritical { get; set; }

        public bool IsActive { get; set; }
    }
}

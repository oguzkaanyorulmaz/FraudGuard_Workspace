namespace FraudGuard.Application.DTOs.TransactionProcessing
{
    /// <summary>
    /// Tetiklenen tek bir kuralın istemciye açılan görünümü.
    /// </summary>
    public class TriggeredRuleDto
    {
        public string RuleCode { get; set; } = string.Empty;
        public string RuleName { get; set; } = string.Empty;
        public int Score { get; set; }
        public string Target { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string? Reason { get; set; }
    }

    /// <summary>
    /// Uygulanan kombinasyon bonusunun istemciye açılan görünümü.
    /// </summary>
    public class AppliedCombinationDto
    {
        public string CombinationName { get; set; } = string.Empty;
        public string RuleCodes { get; set; } = string.Empty;
        public int BonusScore { get; set; }
        public string? FraudType { get; set; }
    }

    /// <summary>
    /// Değerlendirilemeyen kuralın istemciye açılan görünümü.
    /// Kural yazarken en hızlı geri bildirim kanalı budur.
    /// </summary>
    public class RuleFailureDto
    {
        public string RuleCode { get; set; } = string.Empty;
        public string? Expression { get; set; }
        public string Error { get; set; } = string.Empty;
    }
}

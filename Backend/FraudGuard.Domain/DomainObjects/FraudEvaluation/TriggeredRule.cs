using FraudGuard.Domain.Common.Enums;

namespace FraudGuard.Domain.DomainObjects.FraudEvaluation
{
    /// <summary>
    /// Bir işlemde tetiklenen tek bir kuralın sonucu.
    /// </summary>
    public sealed class TriggeredRule
    {
        public required string RuleCode { get; init; }
        public required string RuleName { get; init; }

        /// <summary>Bu kuralın hedef skoruna eklediği ceza puanı.</summary>
        public required int Score { get; init; }

        public required RuleTargetEnum Target { get; init; }
        public required RuleCategoryEnum Category { get; init; }

        /// <summary>
        /// Kesin/yaptırım kuralı mı. İşaretliyse puanı güven indiriminden muaf tutulur.
        /// </summary>
        public bool IsCritical { get; init; }

        /// <summary>Analiste gösterilecek tetiklenme gerekçesi.</summary>
        public string? Reason { get; init; }
    }
}

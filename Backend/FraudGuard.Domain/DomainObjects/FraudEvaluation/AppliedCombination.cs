using System.Collections.Generic;
using FraudGuard.Domain.Common.Enums;

namespace FraudGuard.Domain.DomainObjects.FraudEvaluation
{
    /// <summary>
    /// Birlikte tetiklenen kurallar nedeniyle uygulanan bonus puan kaydı.
    /// </summary>
    public sealed class AppliedCombination
    {
        public required string CombinationName { get; init; }

        /// <summary>Bonusu tetikleyen kural kodları.</summary>
        public required IReadOnlyList<string> RuleCodes { get; init; }

        public required RuleTargetEnum Target { get; init; }

        /// <summary>Hedef skoruna eklenen bonus puan.</summary>
        public required int BonusScore { get; init; }

        /// <summary>Örüntünün açıklaması. Örn: "Kart test edildi, sonra büyük vuruldu."</summary>
        public string? FraudType { get; init; }
    }
}

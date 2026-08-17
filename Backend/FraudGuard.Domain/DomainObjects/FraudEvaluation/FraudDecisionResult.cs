using System.Collections.Generic;
using System.Linq;
using FraudGuard.Domain.Common.Enums;

namespace FraudGuard.Domain.DomainObjects.FraudEvaluation
{
    /// <summary>
    /// Fraud değerlendirmesinin tam sonucu: kademeli karar, skor kırılımı ve tetiklenen her şey.
    /// Denetlenebilirlik için karar sadece nihai skoru değil, ona nasıl ulaşıldığını da taşır.
    /// </summary>
    public sealed class FraudDecisionResult
    {
        /// <summary>Hedef bazında en yüksek skora göre verilen nihai karar.</summary>
        public RiskDecisionEnum Decision { get; init; } = RiskDecisionEnum.Normal;

        /// <summary>Güven indirimi uygulandıktan sonraki kart risk skoru.</summary>
        public int CardRiskScore { get; init; }

        /// <summary>Güven indirimi uygulandıktan sonraki işyeri risk skoru.</summary>
        public int MerchantRiskScore { get; init; }

        /// <summary>Kararı belirleyen skor: kart ve işyeri skorlarının büyüğü (0 - 100).</summary>
        public int FinalRiskScore => Math.Min(100, CardRiskScore > MerchantRiskScore ? CardRiskScore : MerchantRiskScore);

        /// <summary>İndirim öncesi ham kural puanı toplamı (kart + işyeri).</summary>
        public int RawRuleScore { get; init; }

        /// <summary>Uygulanan toplam kombinasyon bonusu.</summary>
        public int TotalBonusScore { get; init; }

        /// <summary>Uygulanan toplam güven indirimi.</summary>
        public int TotalTrustDiscount { get; init; }

        public IReadOnlyList<TriggeredRule> TriggeredRules { get; init; } = new List<TriggeredRule>();

        public IReadOnlyList<AppliedCombination> AppliedCombinations { get; init; } = new List<AppliedCombination>();

        public IReadOnlyList<string> TrustFactors { get; init; } = new List<string>();

        /// <summary>
        /// Bu değerlendirmede çalıştırılamayan kurallar. Boş olmaması, kural kataloğunda
        /// düzeltilmesi gereken bir tanım olduğu anlamına gelir.
        /// </summary>
        public IReadOnlyList<RuleFailure> Failures { get; init; } = new List<RuleFailure>();

        /// <summary>Herhangi bir kural tetiklendi mi.</summary>
        public bool IsSuspicious => TriggeredRules.Count > 0;

        /// <summary>
        /// En yüksek puanlı tetiklenen kural. Fraud log'u bu kural üzerinden açılır;
        /// tam liste <see cref="TriggeredRules"/> içindedir.
        /// </summary>
        public TriggeredRule? PrimaryRule =>
            TriggeredRules.Count == 0
                ? null
                : TriggeredRules.OrderByDescending(r => r.Score).ThenBy(r => r.RuleCode).First();

        /// <summary>İşlemin reddedilmesi gerekip gerekmediği.</summary>
        public bool ShouldBlock => Decision == RiskDecisionEnum.RetBloke;

        /// <summary>
        /// Analiste ve işlem kaydına yazılacak özet gerekçe.
        /// Tetiklenen tüm kuralları puanlarıyla birlikte tek satırda özetler.
        /// </summary>
        public string BuildReasonSummary()
        {
            if (TriggeredRules.Count == 0)
                return string.Empty;

            var rules = string.Join(", ", TriggeredRules
                .OrderByDescending(r => r.Score)
                .Select(r => $"{r.RuleCode}({r.Score}P)"));

            var summary = $"[{Decision}] Skor {FinalRiskScore} — {rules}";

            if (AppliedCombinations.Count > 0)
            {
                var combos = string.Join(", ", AppliedCombinations.Select(c => $"{c.CombinationName}(+{c.BonusScore}P)"));
                summary += $" | Kombinasyon: {combos}";
            }

            if (TotalTrustDiscount > 0)
                summary += $" | Güven indirimi: -{TotalTrustDiscount}P";

            return summary;
        }

        public static FraudDecisionResult Clean() => new();

        /// <summary>Hiç kural tetiklenmedi, ancak bazı kurallar değerlendirilemedi.</summary>
        public static FraudDecisionResult Clean(IReadOnlyList<RuleFailure> failures) =>
            new() { Failures = failures };
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using FraudGuard.Domain.Common.Constants;
using FraudGuard.Domain.Common.Enums;
using FraudGuard.Domain.DomainObjects.FraudEvaluation;
using FraudGuard.Domain.Interfaces.DomainServices;

namespace FraudGuard.Domain.Services.RuleEngine
{
    /// <summary>
    /// Skorlama politikasının tek sahibi: puanlar nasıl toplanır, güven indirimi nasıl düşülür,
    /// hangi eşikte hangi karar verilir.
    /// <para>
    /// Kart ve işyeri puanları <b>ayrı havuzlarda</b> toplanır ve karar ikisinin toplamına değil
    /// <b>büyüğüne</b> göre verilir; böylece bir taraftaki birikim diğerini haksız yere cezalandırmaz.
    /// </para>
    /// <para>
    /// <see cref="ITrustScoreService"/> ve <see cref="ICombinationEngine"/> gibi saf bir servistir;
    /// veriyi hazır alır, I/O yapmaz. Bu sayede eşik ve indirim davranışı bağımlılıksız test edilebilir.
    /// </para>
    /// </summary>
    public class RiskScoringService : IRiskScoringService
    {
        public FraudDecisionResult BuildDecision(
            RuleEvaluationOutcome outcome,
            IReadOnlyList<AppliedCombination> appliedCombinations,
            TrustAssessment trust)
        {
            var triggeredRules = outcome.Triggered;

            // Kesin kuralların puanı indirimden muaftır; ayrı toplanıp indirim sonrasında eklenir.
            int cardCritical = SumScores(triggeredRules, RuleTargetEnum.Card, critical: true);
            int merchantCritical = SumScores(triggeredRules, RuleTargetEnum.Merchant, critical: true);

            int cardDiscountable = SumScores(triggeredRules, RuleTargetEnum.Card, critical: false)
                                   + SumBonuses(appliedCombinations, RuleTargetEnum.Card);
            int merchantDiscountable = SumScores(triggeredRules, RuleTargetEnum.Merchant, critical: false)
                                       + SumBonuses(appliedCombinations, RuleTargetEnum.Merchant);

            int cardAfterTrust = Floor(cardDiscountable - trust.CardDiscount);
            int merchantAfterTrust = Floor(merchantDiscountable - trust.MerchantDiscount);

            int cardFinal = Math.Min(100, cardAfterTrust + cardCritical);
            int merchantFinal = Math.Min(100, merchantAfterTrust + merchantCritical);

            // Fiilen uygulanan indirim: taban sıfır olduğu için tanımlı indirimden düşük olabilir.
            int appliedDiscount = (cardDiscountable - cardAfterTrust)
                                  + (merchantDiscountable - merchantAfterTrust);

            int cardRaw = cardCritical + SumScores(triggeredRules, RuleTargetEnum.Card, critical: false);
            int merchantRaw = merchantCritical + SumScores(triggeredRules, RuleTargetEnum.Merchant, critical: false);

            int cardBonus = SumBonuses(appliedCombinations, RuleTargetEnum.Card);
            int merchantBonus = SumBonuses(appliedCombinations, RuleTargetEnum.Merchant);

            int decisiveScore = Math.Max(cardFinal, merchantFinal);

            return new FraudDecisionResult
            {
                Decision = ResolveDecision(decisiveScore),
                CardRiskScore = cardFinal,
                MerchantRiskScore = merchantFinal,
                RawRuleScore = cardRaw + merchantRaw,
                TotalBonusScore = cardBonus + merchantBonus,
                TotalTrustDiscount = appliedDiscount,
                TriggeredRules = triggeredRules,
                AppliedCombinations = appliedCombinations,
                TrustFactors = trust.AppliedFactors,
                Failures = outcome.Failures
            };
        }

        private static RiskDecisionEnum ResolveDecision(int score) => score switch
        {
            >= RiskScoringConstants.RetBlokeThreshold => RiskDecisionEnum.RetBloke,
            >= RiskScoringConstants.EkDogrulamaThreshold => RiskDecisionEnum.EkDogrulama,
            >= RiskScoringConstants.IzleThreshold => RiskDecisionEnum.Izle,
            _ => RiskDecisionEnum.Normal
        };

        private static int SumScores(IReadOnlyList<TriggeredRule> rules, RuleTargetEnum target, bool critical) =>
            rules.Where(r => r.Target == target && r.IsCritical == critical).Sum(r => r.Score);

        private static int SumBonuses(IReadOnlyList<AppliedCombination> combinations, RuleTargetEnum target) =>
            combinations.Where(c => c.Target == target).Sum(c => c.BonusScore);

        private static int Floor(int score) =>
            Math.Max(RiskScoringConstants.MinimumRiskScore, score);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using FraudGuard.Domain.DomainObjects.FraudEvaluation;
using FraudGuard.Domain.Entities;
using FraudGuard.Domain.Interfaces.DomainServices;

namespace FraudGuard.Domain.Services.RuleEngine
{
    /// <summary>
    /// Tek başına orta seviyede kalan sinyallerin birlikte görüldüğünde oluşturduğu
    /// fraud örüntülerini yakalar ve bonus puan üretir.
    /// <para>
    /// Bir kombinasyonun uygulanabilmesi için tanımdaki <b>tüm</b> kural kodlarının aynı
    /// değerlendirmede tetiklenmiş olması gerekir. Bonus, hedefin skoruna bir kez eklenir.
    /// </para>
    /// </summary>
    public class CombinationEngine : ICombinationEngine
    {
        private static readonly char[] Separators = { ',', ';' };

        public IReadOnlyList<AppliedCombination> Evaluate(
            IReadOnlyList<TriggeredRule> triggeredRules,
            IReadOnlyList<ERuleCombination> combinations)
        {
            var applied = new List<AppliedCombination>();

            if (triggeredRules.Count < 2 || combinations.Count == 0)
                return applied;

            var triggeredCodes = triggeredRules
                .Select(r => r.RuleCode)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var combination in combinations)
            {
                if (!combination.IsActive)
                    continue;

                var requiredCodes = ParseRuleCodes(combination.RuleCodes);

                if (requiredCodes.Count < 2)
                    continue;

                if (!requiredCodes.All(triggeredCodes.Contains))
                    continue;

                applied.Add(new AppliedCombination
                {
                    CombinationName = combination.CombinationName,
                    RuleCodes = requiredCodes,
                    Target = combination.Target,
                    BonusScore = combination.BonusScore,
                    FraudType = combination.FraudType
                });
            }

            return applied;
        }

        private static List<string> ParseRuleCodes(string rawCodes)
        {
            if (string.IsNullOrWhiteSpace(rawCodes))
                return new List<string>();

            return rawCodes
                .Split(Separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}

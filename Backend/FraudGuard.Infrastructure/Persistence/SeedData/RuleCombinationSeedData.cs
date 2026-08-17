using FraudGuard.Domain.Common.Enums;
using FraudGuard.Domain.Entities;

namespace FraudGuard.Infrastructure.Persistence.SeedData
{
    /// <summary>
    /// Kombinasyon bonuslarının başlangıç verisi.
    /// Tek başına orta seviyede kalan sinyaller birlikte görüldüğünde skoru bir üst karara taşır.
    /// </summary>
    public static class RuleCombinationSeedData
    {
        public static ERuleCombination[] GetCombinations() =>
        [
            new()
            {
                CombinationId = 1,
                CombinationName = "Kart Testi + Cashout",
                RuleCodes = "S3,S5",
                Target = RuleTargetEnum.Card,
                BonusScore = 20,
                FraudType = "Kart önce küçük tutarlarla test edildi, ardından yüksek tutar çekildi.",
                IsActive = true
            },
            new()
            {
                CombinationId = 2,
                CombinationName = "Hız + Gece Aktivitesi",
                RuleCodes = "S1,S4",
                Target = RuleTargetEnum.Card,
                BonusScore = 10,
                FraudType = "Gece saatlerinde yoğunlaşan hızlı işlem serisi.",
                IsActive = true
            },
            new()
            {
                CombinationId = 3,
                CombinationName = "İade Anomalisi",
                RuleCodes = "S7,S8",
                Target = RuleTargetEnum.Card,
                BonusScore = 15,
                FraudType = "İade hacmi ve sıklığı birlikte olağandışı; suistimal örüntüsü.",
                IsActive = true
            },
            new()
            {
                CombinationId = 4,
                CombinationName = "Kart Testi + Ardışık Red",
                RuleCodes = "CARD_TESTING,BRUTE_FORCE",
                Target = RuleTargetEnum.Card,
                BonusScore = 20,
                FraudType = "Deneme çekimleri ile şifre/CVV zorlaması aynı anda görülüyor.",
                IsActive = true
            },
            new()
            {
                CombinationId = 5,
                CombinationName = "Limit Boşaltma + Sınır Ötesi",
                RuleCodes = "MAX_OUT,CROSS_BORDER",
                Target = RuleTargetEnum.Card,
                BonusScore = 15,
                FraudType = "Yurt dışından limit boşaltma denemesi.",
                IsActive = true
            }
        ];
    }
}

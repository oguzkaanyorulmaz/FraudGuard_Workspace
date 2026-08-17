using FraudGuard.Domain.Common.Enums;

namespace FraudGuard.Domain.Entities
{
    /// <summary>
    /// Birden fazla kuralın aynı işlemde birlikte tetiklenmesi durumunda uygulanan bonus puan tanımı.
    /// Tek başına anlamlı olmayan sinyallerin birleşiminde ortaya çıkan fraud örüntülerini yakalar.
    /// </summary>
    public class ERuleCombination
    {
        public int CombinationId { get; set; }

        /// <summary>Örn: "Kart Testi + Cashout".</summary>
        public string CombinationName { get; set; } = string.Empty;

        /// <summary>
        /// Bonusun uygulanması için tetiklenmesi gereken kural kodları, virgülle ayrılmış.
        /// Örn: <c>"S23,S37"</c>. Listedeki <b>tüm</b> kodlar tetiklenmiş olmalıdır.
        /// </summary>
        public string RuleCodes { get; set; } = string.Empty;

        /// <summary>Bonusun yazılacağı risk havuzu.</summary>
        public RuleTargetEnum Target { get; set; } = RuleTargetEnum.Card;

        /// <summary>Hedefin skoruna bir kez eklenecek bonus puan.</summary>
        public int BonusScore { get; set; }

        /// <summary>Analiste gösterilecek örüntü açıklaması.</summary>
        public string? FraudType { get; set; }

        public bool IsActive { get; set; } = true;
    }
}

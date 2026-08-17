using FraudGuard.Domain.Common.Enums;

namespace FraudGuard.Domain.Entities
{
    /// <summary>
    /// Kural tanımı. İki tip kuralı tek tabloda taşır:
    /// <list type="bullet">
    /// <item><b>Dinamik kural:</b> <see cref="Expression"/> doludur. İfade çalışma anında derlenip
    /// çalıştırılır, ayrı bir .cs dosyası gerektirmez.</item>
    /// <item><b>Kod tabanlı kural:</b> <see cref="Expression"/> boştur. <see cref="RuleCode"/> ile eşleşen
    /// <c>IFraudRule</c> implementasyonu çalıştırılır. Mevcut kurallar bu yolla korunur.</item>
    /// </list>
    /// </summary>
    public class EFraudRule
    {
        public int RuleId { get; set; }

        /// <summary>Kuralın benzersiz kodu. Örn: "S1", "VELOCITY".</summary>
        public string RuleCode { get; set; } = string.Empty;

        public string RuleName { get; set; } = string.Empty;

        public string? Description { get; set; }

        /// <summary>
        /// Çalışma anında derlenen boolean ifade. Tek parametresi <c>input</c> olup
        /// <c>ProcessTransactionInput</c> örneğine bağlanır.
        /// Örn: <c>input.FarkliKartSayisi &gt;= 3 || input.AyniKartIslemAdedi &gt;= 3</c>
        /// <para>Boş bırakılırsa kural kod tabanlı olarak değerlendirilir.</para>
        /// </summary>
        public string? Expression { get; set; }

        /// <summary>Kural tetiklendiğinde hedefin risk skoruna eklenecek ceza puanı.</summary>
        public int Score { get; set; }

        /// <summary>Puanın kart havuzuna mı yoksa işyeri havuzuna mı yazılacağı.</summary>
        public RuleTargetEnum Target { get; set; } = RuleTargetEnum.Card;

        /// <summary>Raporlama amaçlı fraud tipolojisi.</summary>
        public RuleCategoryEnum Category { get; set; } = RuleCategoryEnum.Velocity;

        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Kuralın dinamik ifade ile mi değerlendirileceğini belirtir.
        /// Kalıcı değildir; <see cref="Expression"/> alanından türetilir.
        /// </summary>
        public bool IsExpressionBased => !string.IsNullOrWhiteSpace(Expression);
    }
}

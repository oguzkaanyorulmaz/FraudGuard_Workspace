using FraudGuard.Domain.Common.Enums;

namespace FraudGuard.Domain.Entities
{
    /// <summary>
    /// Kural tanımı. Her kural bir <see cref="Expression"/> taşır; ifade çalışma anında derlenip
    /// çalıştırılır, ayrı bir .cs dosyası gerektirmez. Yeni kural eklemek için bu tabloya
    /// satır eklemek yeterlidir.
    /// <para>
    /// İfadesi boş bırakılan kural değerlendirilemez ve tanım hatası olarak raporlanır.
    /// </para>
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
        /// <para>
        /// Zorunludur. Boş bırakılan kural değerlendirilemez; motor onu atlar ve tanım hatası
        /// olarak <c>ruleFailures</c>'a yazar.
        /// </para>
        /// </summary>
        public string? Expression { get; set; }

        /// <summary>Kural tetiklendiğinde hedefin risk skoruna eklenecek ceza puanı.</summary>
        public int Score { get; set; }

        /// <summary>Puanın kart havuzuna mı yoksa işyeri havuzuna mı yazılacağı.</summary>
        public RuleTargetEnum Target { get; set; } = RuleTargetEnum.Card;

        /// <summary>Raporlama amaçlı fraud tipolojisi.</summary>
        public RuleCategoryEnum Category { get; set; } = RuleCategoryEnum.Velocity;

        /// <summary>
        /// Kesin/yaptırım kuralı mı. İşaretliyse bu kuralın puanı <b>güven indiriminden muaftır</b>.
        /// <para>
        /// Gerekçe: whitelist veya temiz geçmiş, kara listedeki bir hesaba gönderim gibi
        /// deterministik bir yaptırım sinyalini bastırmamalıdır. Sezgisel (heuristic) kurallar
        /// için işaretlenmemelidir; aksi hâlde güven skoru anlamını yitirir.
        /// </para>
        /// </summary>
        public bool IsCritical { get; set; }

        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Kuralın dinamik ifade ile mi değerlendirileceğini belirtir.
        /// Kalıcı değildir; <see cref="Expression"/> alanından türetilir.
        /// </summary>
        public bool IsExpressionBased => !string.IsNullOrWhiteSpace(Expression);
    }
}

using System.Collections.Generic;

namespace FraudGuard.Domain.DomainObjects.FraudEvaluation
{
    /// <summary>
    /// Güven skoru değerlendirmesinin sonucu. İndirimler risk skorundan düşülür.
    /// </summary>
    public sealed class TrustAssessment
    {
        /// <summary>Kart risk skorundan düşülecek toplam indirim.</summary>
        public int CardDiscount { get; init; }

        /// <summary>İşyeri risk skorundan düşülecek toplam indirim.</summary>
        public int MerchantDiscount { get; init; }

        /// <summary>İndirimi oluşturan faktörlerin okunabilir listesi.</summary>
        public IReadOnlyList<string> AppliedFactors { get; init; } = new List<string>();

        public static TrustAssessment None => new();
    }

    /// <summary>
    /// Güven skoru hesabı için gereken ham gerçekler.
    /// Veriyi orkestratör toplar; hesabı <c>TrustScoreService</c> yapar.
    /// Bu ayrım, domain servisini repository bağımlılığından uzak tutar.
    /// </summary>
    public sealed class TrustContext
    {
        /// <summary>Kart hamilinin sistemdeki kayıt süresi (gün). Bilinmiyorsa null.</summary>
        public int? CardHolderTenureDays { get; init; }

        /// <summary>Kartın son 90 gündeki fraud alarmı sayısı. Bilinmiyorsa null.</summary>
        public int? CardAlarmCountLast90Days { get; init; }

        /// <summary>Kart manuel olarak whitelist'e alınmış mı.</summary>
        public bool IsCardWhitelisted { get; init; }

        /// <summary>İşyerinin sistemdeki kayıt süresi (gün). Merchant verisi yoksa null.</summary>
        public int? MerchantTenureDays { get; init; }

        /// <summary>İşyerinin son 90 gündeki alarm sayısı. Merchant verisi yoksa null.</summary>
        public int? MerchantAlarmCountLast90Days { get; init; }

        /// <summary>İşyeri manuel olarak whitelist'e alınmış mı.</summary>
        public bool IsMerchantWhitelisted { get; init; }
    }
}

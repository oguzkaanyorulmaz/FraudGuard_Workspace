namespace FraudGuard.Domain.Common.Constants
{
    /// <summary>
    /// Kümülatif skorlama motorunun eşik ve indirim sabitleri.
    /// Tek kaynak burasıdır; motorlar bu değerleri literal olarak tekrarlamaz.
    /// </summary>
    public static class RiskScoringConstants
    {
        // --- 4 kademeli karar matrisi (alt sınırlar, dahil) ---

        /// <summary>40 puandan itibaren analist paneline alarm düşer.</summary>
        public const int IzleThreshold = 40;

        /// <summary>70 puandan itibaren 3D Secure / OTP zorunludur.</summary>
        public const int EkDogrulamaThreshold = 70;

        /// <summary>90 puandan itibaren işlem reddedilir ve hedef bloke edilir.</summary>
        public const int RetBlokeThreshold = 90;

        // --- Güven skoru indirimleri (risk skorundan düşülür) ---

        /// <summary>İşyeri 6 aydan uzun süredir sistemde kayıtlı.</summary>
        public const int TrustDiscountEstablishedMerchant = 15;

        /// <summary>Son 90 günde hiç alarm/fraud kaydı yok.</summary>
        public const int TrustDiscountNoRecentAlarm = 20;

        /// <summary>Manuel olarak whitelist'e alınmış hedef.</summary>
        public const int TrustDiscountWhitelisted = 40;

        /// <summary>"Yerleşik işyeri" sayılmak için gereken asgari kayıt süresi (gün).</summary>
        public const int EstablishedMerchantMinDays = 180;

        /// <summary>Alarm geçmişinin tarandığı pencere (gün).</summary>
        public const int NoAlarmLookbackDays = 90;

        /// <summary>
        /// Nihai skorun alabileceği en düşük değer. Güven indirimi skoru negatife düşüremez.
        /// </summary>
        public const int MinimumRiskScore = 0;
    }
}

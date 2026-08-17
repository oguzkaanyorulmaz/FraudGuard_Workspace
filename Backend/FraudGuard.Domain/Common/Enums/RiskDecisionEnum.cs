namespace FraudGuard.Domain.Common.Enums
{
    /// <summary>
    /// Kümülatif risk skoruna göre üretilen 4 kademeli nihai karar.
    /// Eşik değerleri <see cref="Common.Constants.RiskScoringConstants"/> içinde tanımlıdır.
    /// </summary>
    public enum RiskDecisionEnum
    {
        /// <summary>0-39 puan: İşleme izin verilir, aksiyon yok.</summary>
        Normal = 0,

        /// <summary>40-69 puan: İşlem geçer ancak analist paneline sarı alarm düşer.</summary>
        Izle = 1,

        /// <summary>70-89 puan: 3D Secure / OTP gibi ek doğrulama zorunlu tutulur.</summary>
        EkDogrulama = 2,

        /// <summary>90 ve üzeri: İşlem anında reddedilir, hedef bloke edilir.</summary>
        RetBloke = 3
    }
}

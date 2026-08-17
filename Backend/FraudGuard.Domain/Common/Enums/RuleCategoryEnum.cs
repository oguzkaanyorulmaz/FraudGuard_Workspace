namespace FraudGuard.Domain.Common.Enums
{
    /// <summary>
    /// Kuralın fraud tipolojisi. Raporlama ve analist panelinde gruplama için kullanılır,
    /// skorlamayı etkilemez.
    /// </summary>
    public enum RuleCategoryEnum
    {
        /// <summary>Hız / sıklık / adet bazlı örüntüler.</summary>
        Velocity = 1,

        /// <summary>Tutar eşiği ve tutar anomalisi bazlı örüntüler.</summary>
        Amount = 2,

        /// <summary>Zaman penceresi ve saat bazlı örüntüler.</summary>
        Time = 3,

        /// <summary>Kart hamili / işyeri kimlik tutarsızlıkları.</summary>
        Identity = 4,

        /// <summary>Coğrafi konum, ülke ve sınır ötesi örüntüler.</summary>
        Location = 5
    }
}

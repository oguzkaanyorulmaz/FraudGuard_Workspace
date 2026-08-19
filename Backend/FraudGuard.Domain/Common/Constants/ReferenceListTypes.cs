namespace FraudGuard.Domain.Common.Constants
{
    /// <summary>
    /// <see cref="Entities.EReferenceListEntry.ListType"/> için tanımlı değerler.
    /// Sabit tutulur ki liste türü yazım hatasıyla sessizce boş kalmasın.
    /// </summary>
    public static class ReferenceListTypes
    {
        /// <summary>İşlemleri durdurulan ülkeler (ISO alpha-2). S57.</summary>
        public const string BlockedCountry = "BLOCKED_COUNTRY";

        /// <summary>Riskli sayılan kart ihraç ülkeleri. S42.</summary>
        public const string RiskyCountry = "RISKY_COUNTRY";

        /// <summary>İşlemleri durdurulan kart şemaları. S56.</summary>
        public const string BlockedScheme = "BLOCKED_SCHEME";

        /// <summary>Şifresiz işleme kapalı MCC'ler. S49.</summary>
        public const string PinlessBlockedMcc = "PINLESS_BLOCKED_MCC";

        /// <summary>Kuyumcu/değerli maden MCC'leri. S46.</summary>
        public const string JewelryMcc = "JEWELRY_MCC";
    }
}

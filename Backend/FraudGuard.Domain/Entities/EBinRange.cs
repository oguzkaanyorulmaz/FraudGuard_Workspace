namespace FraudGuard.Domain.Entities
{
    /// <summary>
    /// Kart BIN (ilk 6 hane) kaydı: kartı ihraç eden kurumun ülkesi, şeması ve risk işaretleri.
    /// <para>
    /// Kart numarasının kendisi kartın nereden geldiğini söylemez; BIN tablosu olmadan
    /// "yurtdışı kart", "riskli ülke kartı" veya "yasaklı BIN" tipolojileri değerlendirilemez.
    /// </para>
    /// </summary>
    public class EBinRange
    {
        /// <summary>Kart numarasının ilk 6 hanesi. Doğal anahtardır.</summary>
        public string BinPrefix { get; set; } = string.Empty;

        /// <summary>ISO 3166-1 alpha-2 ülke kodu. Örn: "TR", "IQ", "US".</summary>
        public string CountryCode { get; set; } = "TR";

        /// <summary>Kart şeması. Örn: "TROY", "VISA", "MASTERCARD", "AMEX".</summary>
        public string Scheme { get; set; } = string.Empty;

        public string? BankName { get; set; }

        /// <summary>Kuruma özel riskli BIN işareti (S41).</summary>
        public bool IsRisky { get; set; }

        /// <summary>Yaptırım listesindeki BIN (S47). Kesin kural olarak değerlendirilir.</summary>
        public bool IsSanctioned { get; set; }

        /// <summary>Belirli bir aracı kuruma ait BIN grubu (S50).</summary>
        public bool IsExpedia { get; set; }

        public bool IsActive { get; set; } = true;
    }
}

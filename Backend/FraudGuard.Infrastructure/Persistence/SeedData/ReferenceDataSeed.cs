using FraudGuard.Domain.Common.Constants;
using FraudGuard.Domain.Entities;

namespace FraudGuard.Infrastructure.Persistence.SeedData
{
    /// <summary>
    /// BIN ve operasyonel liste başlangıç verisi.
    /// <para>
    /// Gerçek BIN tablosu kurumdan temin edilir; buradaki kayıtlar sistemin çalışabilmesi
    /// ve senaryoların test edilebilmesi için konulmuş <b>örnek</b> verilerdir.
    /// Üretime geçerken bu liste kurumun BIN dosyasıyla değiştirilmelidir.
    /// </para>
    /// </summary>
    public static class ReferenceDataSeed
    {
        public static EBinRange[] GetBinRanges() =>
        [
            // Seed kartlarının BIN'i — yerli, TROY. Normal davranışın referansı.
            Bin("552000", "TR", "MASTERCARD", "FraudGuard Test Bank"),
            Bin("979200", "TR", "TROY", "FraudGuard Test Bank"),
            Bin("454360", "TR", "VISA", "FraudGuard Test Bank"),

            // Yurtdışı — yaptırım veya risk işareti yok.
            Bin("411111", "US", "VISA", "Example US Issuer"),
            Bin("520000", "DE", "MASTERCARD", "Example DE Issuer"),

            // Riskli ülke kartı (S42/S46): Irak ihraçlı.
            Bin("627890", "IQ", "MASTERCARD", "Example IQ Issuer"),

            // Kuruma özel riskli BIN (S41).
            Bin("510510", "RU", "MASTERCARD", "Example Risky Issuer", isRisky: true),

            // Yaptırım listesindeki BIN (S47) — kesin kural.
            Bin("400000", "IR", "VISA", "Sanctioned Issuer", isSanctioned: true),

            // Aracı kurum BIN grubu (S50).
            Bin("559999", "US", "MASTERCARD", "Expedia Virtual Card", isExpedia: true)
        ];

        public static EReferenceListEntry[] GetListEntries() =>
        [
            Entry(1, ReferenceListTypes.RiskyCountry, "IQ", "Irak ihraçlı kartlar"),
            Entry(2, ReferenceListTypes.RiskyCountry, "IR", "İran ihraçlı kartlar"),
            Entry(3, ReferenceListTypes.RiskyCountry, "RU", "Rusya ihraçlı kartlar"),

            Entry(4, ReferenceListTypes.BlockedCountry, "KP", "İşlemleri durdurulan ülke"),

            Entry(5, ReferenceListTypes.BlockedScheme, "DISCOVER", "İşlemleri durdurulan şema"),

            Entry(6, ReferenceListTypes.PinlessBlockedMcc, "5944", "Kuyumcu"),
            Entry(7, ReferenceListTypes.PinlessBlockedMcc, "5094", "Değerli maden"),
            Entry(8, ReferenceListTypes.PinlessBlockedMcc, "7995", "Şans oyunları"),
            Entry(9, ReferenceListTypes.PinlessBlockedMcc, "6051", "Kripto / döviz"),

            Entry(10, ReferenceListTypes.JewelryMcc, "5944", "Kuyumcu"),
            Entry(11, ReferenceListTypes.JewelryMcc, "5094", "Değerli maden")
        ];

        private static EBinRange Bin(
            string prefix, string country, string scheme, string bank,
            bool isRisky = false, bool isSanctioned = false, bool isExpedia = false) =>
            new()
            {
                BinPrefix = prefix,
                CountryCode = country,
                Scheme = scheme,
                BankName = bank,
                IsRisky = isRisky,
                IsSanctioned = isSanctioned,
                IsExpedia = isExpedia,
                IsActive = true
            };

        private static EReferenceListEntry Entry(int id, string type, string value, string description) =>
            new()
            {
                EntryId = id,
                ListType = type,
                Value = value,
                Description = description,
                IsActive = true
            };
    }
}

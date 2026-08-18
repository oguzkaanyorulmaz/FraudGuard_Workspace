using System;
using FraudGuard.Domain.Entities;

namespace FraudGuard.Infrastructure.Persistence.SeedData
{
    /// <summary>
    /// Üye işyeri başlangıç verisi.
    /// <para>
    /// <see cref="EMerchant.MerchantCategory"/> değerleri simülatördeki kategori listesiyle
    /// birebir aynıdır; işyeri seçildiğinde kategori de tutarlı şekilde dolar.
    /// </para>
    /// <para>
    /// POS tahsis tarihleri kurulum anına göreli verilir. Sabit tarih kullanılsaydı
    /// "yeni işyeri" kavramı zamanla anlamını yitirir, ilgili tipoloji test edilemez hale gelirdi.
    /// </para>
    /// </summary>
    public static class MerchantSeedData
    {
        public static EMerchant[] GetMerchants()
        {
            var today = DateTime.Today;

            return
            [
                Merchant("MRC001", "Yıldız Market", "5411", "Market", today.AddDays(-1450), "İstanbul"),
                Merchant("MRC002", "TeknoDünya Elektronik", "5732", "Elektronik", today.AddDays(-980), "Ankara"),
                Merchant("MRC003", "Moda Sokak Giyim", "5651", "Giyim", today.AddDays(-720), "İzmir"),
                Merchant("MRC004", "Lezzet Durağı Restoran", "5812", "Restoran", today.AddDays(-610), "İstanbul"),
                Merchant("MRC005", "Anadolu Akaryakıt", "5541", "Akaryakıt", today.AddDays(-1200), "Konya"),
                Merchant("MRC006", "Mavi Tur Seyahat", "4722", "Seyahat", today.AddDays(-540), "Antalya"),
                Merchant("MRC007", "Deniz Otel", "7011", "Konaklama", today.AddDays(-830), "Antalya"),
                Merchant("MRC008", "Özsoy Kuyumculuk", "5944", "Kuyumcu", today.AddDays(-395), "İstanbul"),
                Merchant("MRC009", "Sağlık Merkezi Poliklinik", "8062", "Sağlık", today.AddDays(-1100), "Bursa"),
                Merchant("MRC010", "Akademi Eğitim Kurumu", "8220", "Eğitim", today.AddDays(-660), "Ankara"),
                Merchant("MRC011", "HızlıAl E-Ticaret", "5999", "E-Ticaret", today.AddDays(-300), "İstanbul"),
                Merchant("MRC012", "Sahne Eğlence Merkezi", "7996", "Eğlence", today.AddDays(-450), "İzmir"),

                // Yüksek riskli kategoriler — MCC bazlı kuralları denemek için.
                Merchant("MRC013", "Kripto Değişim Noktası", "6051", "Kripto", today.AddDays(-210), "İstanbul"),
                Merchant("MRC014", "Şans Oyunları Bayi", "7995", "Bahis", today.AddDays(-160), "Ankara"),

                // POS'u yeni tahsis edilmiş işyerleri — "yeni işyeri ani ciro" tipolojisi için.
                Merchant("MRC015", "Yeni Nesil Elektronik", "5732", "Elektronik", today.AddDays(-9), "İstanbul"),
                Merchant("MRC016", "Taze Gıda Marketi", "5411", "Market", today.AddDays(-3), "Kocaeli")
            ];
        }

        private static EMerchant Merchant(
            string id, string name, string mcc, string category, DateTime posAssignedAt, string city) =>
            new()
            {
                MerchantId = id,
                MerchantName = name,
                MccCode = mcc,
                MerchantCategory = category,
                PosAssignmentDate = posAssignedAt,
                City = city,
                Country = "Türkiye",
                IsActive = true
            };
    }
}

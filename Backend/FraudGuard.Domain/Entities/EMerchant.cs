using System;

namespace FraudGuard.Domain.Entities
{
    /// <summary>
    /// Üye işyeri (merchant) master verisi. İşlem, işyerine <see cref="MerchantId"/> ile bağlanır.
    /// <para>
    /// Bu varlık olmadan işyeri bazlı sayaçlar hesaplanamaz: bir POS'ta kaç farklı kart görüldüğü,
    /// işyerinin MCC'si veya POS'un ne zaman tahsis edildiği yalnızca buradan bilinebilir.
    /// </para>
    /// </summary>
    public class EMerchant
    {
        /// <summary>İşyeri kodu. Kısa ve okunabilir olması için doğal anahtar kullanılır. Örn: "MRC001".</summary>
        public string MerchantId { get; set; } = string.Empty;

        public string MerchantName { get; set; } = string.Empty;

        /// <summary>Merchant Category Code (ISO 18245). Örn: "5944" = Kuyumcu.</summary>
        public string MccCode { get; set; } = string.Empty;

        /// <summary>
        /// İşlem üzerindeki serbest metin kategoriyle aynı sözlüğü kullanır ("Kuyumcu", "Market"…).
        /// Mevcut kategori bazlı kuralların işyeri seçimiyle tutarlı kalmasını sağlar.
        /// </summary>
        public string MerchantCategory { get; set; } = string.Empty;

        /// <summary>
        /// POS'un işyerine tahsis edildiği tarih. "Yeni işyeri ani yüksek ciro" tipolojisi
        /// bu tarihe göre değerlendirilir.
        /// </summary>
        public DateTime PosAssignmentDate { get; set; }

        public string City { get; set; } = string.Empty;
        public string Country { get; set; } = "Türkiye";

        /// <summary>
        /// İşyeri vergi mükellefi mi. Sınır ötesi işlemlerde mükellef olmayan işyerleri
        /// yaptırıma tabidir (S44).
        /// </summary>
        public bool IsTaxpayer { get; set; } = true;

        /// <summary>
        /// İşyeri yetkilisinin doğum tarihi. Yaşça küçük yetkiliye sahip firmalar
        /// ayrı bir risk tipolojisidir (S54).
        /// </summary>
        public DateTime? OwnerBirthDate { get; set; }

        /// <summary>İşyerine yurtdışı kartlarla işlem yapılması yasak mı (S43).</summary>
        public bool ForeignCardsBlocked { get; set; }

        /// <summary>Ödeme kolaylaştırıcı altındaki alt üye işyeri mi (S45).</summary>
        public bool IsPaymentFacilitatorSub { get; set; }

        public bool IsActive { get; set; } = true;
    }
}

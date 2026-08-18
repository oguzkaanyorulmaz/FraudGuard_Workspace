using System;
using FraudGuard.Domain.Common.Enums;

namespace FraudGuard.Domain.DomainObjects.TransactionProcessing
{
    /// <summary>
    /// Fraud değerlendirmesine giren zenginleştirilmiş işlem modeli.
    /// Dinamik kural ifadelerinde <c>input</c> adıyla bağlanır; ifadeler yalnızca buradaki
    /// public property'lere erişebilir.
    /// <para>
    /// Alanlar üç gruba ayrılır:
    /// <list type="number">
    /// <item><b>Ham işlem alanları</b> — istekten gelir.</item>
    /// <item><b>Türetilmiş zaman alanları</b> — <c>TransactionInputEnricher</c> doldurur.</item>
    /// <item><b>Sayaçlar</b> — <c>TransactionInputEnricher</c> işlem geçmişinden hesaplar.</item>
    /// </list>
    /// </para>
    /// </summary>
    public class ProcessTransactionInput
    {
        // ------------------------------------------------------------------
        // 1. Ham işlem alanları
        // ------------------------------------------------------------------

        public string? CardNumber { get; set; }
        public string? ExpiryDate { get; set; }
        public string? CVV { get; set; }

        public string? SenderIBAN { get; set; }
        public string? ReceiverIBAN { get; set; }
        public string? ReceiverName { get; set; }
        public string? Description { get; set; }

        public decimal Amount { get; set; }
        public string Currency { get; set; } = "TRY";
        public TransactionTypeEnum TransactionType { get; set; }
        public PaymentTypeEnum PaymentType { get; set; }
        public int ChannelTypeId { get; set; }

        public string Location { get; set; } = string.Empty;
        public string Country { get; set; } = "Türkiye";
        public string MerchantCategory { get; set; } = "Diğer";
        public int? OriginalTransactionId { get; set; }
        public string? RRN { get; set; }

        // ------------------------------------------------------------------
        // 2. Türetilmiş zaman alanları
        // ------------------------------------------------------------------

        /// <summary>İşlemin değerlendirmeye alındığı an. Enricher tarafından set edilir.</summary>
        public DateTime IslemZamani { get; set; } = DateTime.Now;

        /// <summary>İşlem saati (0-23). Saat bazlı ifadeleri okunaklı kılar.</summary>
        public int IslemSaati { get; set; }

        /// <summary>İşlem cumartesi veya pazar günü mü gerçekleşti.</summary>
        public bool HaftaSonuMu { get; set; }

        /// <summary>İşlem gece penceresinde mi (22:00-06:00).</summary>
        public bool GeceIslemiMi { get; set; }

        // ------------------------------------------------------------------
        // 3. Sayaçlar — kart bazlı, son 24 saatlik geçmişten hesaplanır
        // ------------------------------------------------------------------

        /// <summary>Aynı kartla son 1 saatteki işlem adedi (başarılı + başarısız).</summary>
        public int AyniKartIslemAdedi { get; set; }

        /// <summary>Aynı kartla son 2 dakikadaki işlem adedi.</summary>
        public int IkiDakikadaYapilanIslemAdedi { get; set; }

        /// <summary>Son 24 saatte onaylanmayan (Declined/Suspicious) işlem adedi.</summary>
        public int BasarisizIslemSayisi { get; set; }

        /// <summary>Son 24 saatteki toplam işlem adedi.</summary>
        public int SonGunIslemSayisi { get; set; }

        /// <summary>Son 24 saatteki toplam işlem hacmi (TRY).</summary>
        public decimal SonGunIslemHacmi { get; set; }

        /// <summary>Son 24 saatteki onaylı satış tutarı toplamı.</summary>
        public decimal ToplamSatisTutar { get; set; }

        /// <summary>Son 24 saatteki iade tutarı toplamı.</summary>
        public decimal ToplamIadeTutari { get; set; }

        /// <summary>Son 2 saatteki iade işlemi adedi.</summary>
        public int IkiSaatlikIadeIslemSayisi { get; set; }

        /// <summary>Son 1 saatte gerçekleşen 1.000 TL altı onaylı işlem adedi.</summary>
        public int BinAltindaOnayliIslemAdedi { get; set; }

        /// <summary>Son 24 saatte bu işlemle aynı tutarda gerçekleşen işlem adedi.</summary>
        public int AyniKartAyniTutarAdedi { get; set; }

        /// <summary>Son 24 saatte işlem görülen farklı işyeri kategorisi sayısı.</summary>
        public int FarkliKategoriSayisi { get; set; }

        /// <summary>Son 24 saatte işlem görülen farklı ülke sayısı.</summary>
        public int FarkliUlkeSayisi { get; set; }

        /// <summary>Son 24 saatte gece penceresinde gerçekleşen işlem adedi.</summary>
        public int GeceIslemAdedi { get; set; }

        /// <summary>Son 24 saatteki en yüksek tekil işlem tutarı.</summary>
        public decimal EnYuksekIslemTutari { get; set; }

        /// <summary>Son 24 saatteki ortalama işlem tutarı. Geçmiş yoksa 0.</summary>
        public decimal OrtalamaIslemTutari { get; set; }

        /// <summary>
        /// Bir önceki işlemden bu yana geçen dakika. Geçmiş yoksa <see cref="int.MaxValue"/>.
        /// </summary>
        public int SonIslemdenGecenDakika { get; set; } = int.MaxValue;

        // ------------------------------------------------------------------
        // 4. Kart Limit & Bakiye Durumu (Türetilmiş)
        // ------------------------------------------------------------------
        public decimal KartLimiti { get; set; }
        public decimal KalanLimit { get; set; }
        /// <summary>Kalan limitin toplam limite oranı (0.00 - 1.00). Limit sıfırsa 1.0.</summary>
        public decimal KalanLimitOrani { get; set; } = 1.0m;
        /// <summary>İşlemin kart limitine oranı (Amount / KartLimiti).</summary>
        public decimal LimitKullanimOrani { get; set; }
        /// <summary>İşlemin hesap bakiyesine oranı (Amount / Bakiye).</summary>
        public decimal BakiyeCekimOrani { get; set; }

        // ------------------------------------------------------------------
        // 5. Transfer, Alıcı & Güvenlik Göstergeleri (Türetilmiş)
        // ------------------------------------------------------------------
        /// <summary>Açıklama alanında yasaklı (bahis, kripto, kumar vb.) kelime var mı.</summary>
        public bool YasakliKelimeIceriyorMu { get; set; }
        /// <summary>Alıcı IBAN daha önce sistemde riskli/katır olarak etiketlenmiş mi.</summary>
        public bool RiskliAliciMi { get; set; }
        /// <summary>Alıcı müşterinin alıcı listesine yeni (son 5 dk) mi eklendi.</summary>
        public bool YeniAliciMi { get; set; }
        /// <summary>Mevcut ülke Türkiye dışında yabancı bir ülke mi.</summary>
        public bool YabanciUlkeMi { get; set; }
        /// <summary>Kart geçmişinde bu ülke hiç kullanılmamış mı.</summary>
        public bool GecmisteKullanilmayanUlkeMi { get; set; }
        /// <summary>Kart geçmişinde bu para birimi hiç kullanılmamış mı.</summary>
        public bool GecmisteKullanilmayanParaBirimiMi { get; set; }
        /// <summary>Kuyumcu, bahis, kripto vb. yüksek riskli MCC mi.</summary>
        public bool RiskliMccMi { get; set; }
        /// <summary>Son iki işlem arasındaki coğrafi mesafe ve süre imkansız seyahat oluşturuyor mu.</summary>
        public bool ImkansizSeyahatVarMi { get; set; }
        /// <summary>ATM'den nakit yatırıldıktan sonraki 1 saatte harcanma/çekilme oranı.</summary>
        public decimal NakitYatirmaSonrasiHarcanmaOrani { get; set; }
        /// <summary>Son 24 saatte ATM'lerden yatırılan toplam nakit tutarı.</summary>
        public decimal SonGunAtmNakitYatirmaHacmi { get; set; }
        /// <summary>Son 24 saatte ATM'lerden nakit yatırma işlem adedi.</summary>
        public int SonGunAtmNakitYatirmaSayisi { get; set; }
        /// <summary>Gece saatlerinde ATM'den yatırılan nakit tutarı.</summary>
        public decimal GeceNakitYatirmaHacmi { get; set; }
        /// <summary>Aynı alıcı IBAN'a son 1 saatte para gönderen farklı gönderici sayısı.</summary>
        public int Son1SaatFarkliGondericiSayisi { get; set; }
        /// <summary>Cüzdana yükleme yapıldıktan hemen sonra EFT ile çıkış deneniyor mu.</summary>
        public bool CuzdanFonlamaSonrasiNakitCikisVarMi { get; set; }
        /// <summary>Aynı cüzdana kısa sürede farklı kartlarla bakiye yükleme sayısı.</summary>
        public int KisaSuredeFarkliKartlaFonlamaSayisi { get; set; }
        /// <summary>Pasif hesaba aniden para gelip hızla çekilme anormalliği var mı.</summary>
        public bool KatirHesapBakiyeAnormalligiVarMi { get; set; }

        // ------------------------------------------------------------------
        // 6. İşyeri bazlı alanlar
        //    İstekte MerchantId gönderildiğinde EMerchant kaydından ve işyeri
        //    geçmişinden doldurulur. İşyeri seçilmeyen işlemlerde varsayılan değerlerde kalır.
        // ------------------------------------------------------------------

        /// <summary>İşlemin geçtiği üye işyerinin kodu. İstekten gelir.</summary>
        public string? MerchantId { get; set; }

        /// <summary>İşyerinin MCC kodu (ISO 18245). İşyeri kaydından okunur.</summary>
        public string? MccKodu { get; set; }

        /// <summary>Bu işyerinde son 1 saatte işlem yapan farklı kart sayısı (bu kart dahil).</summary>
        public int FarkliKartSayisi { get; set; }

        /// <summary>Bu kartın son 24 saatte işlem yaptığı farklı işyeri sayısı (bu işyeri dahil).</summary>
        public int FarkliIsyeriSayisi { get; set; }

        /// <summary>İşyerinin POS tahsis tarihi. İşyeri kaydından okunur.</summary>
        public DateTime? PosTahsisTarihi { get; set; }

        /// <summary>
        /// POS tahsisinden bu yana geçen gün. İşyeri bilinmiyorsa <see cref="int.MaxValue"/>.
        /// <see cref="PosTahsisTarihi"/> ile aynı bilgiyi taşır ama ifadelerde nullable
        /// tarih aritmetiği gerektirmediği için kural yazarken tercih edilmelidir.
        /// </summary>
        public int IsyeriYasiGun { get; set; } = int.MaxValue;
    }
}

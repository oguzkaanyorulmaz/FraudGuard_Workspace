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
        /// <summary>
        /// Yetkilendirme mesajının ham alanları (AUTHFINANCIALAPPROVE).
        /// İfadelerde <c>input.Auth.PinExist == false</c> şeklinde kullanılır.
        /// <para>
        /// Kökteki alanlar hesaplanmış/dönüştürülmüş değerlerdir; buradakiler ham gelen veridir.
        /// Karşılığı kökte varsa (Amount, Location, RRN, MerchantId, MccKodu) kök tercih edilmelidir.
        /// </para>
        /// </summary>
        public AuthMessageFields Auth { get; set; } = new();

        // ------------------------------------------------------------------
        // İşyeri bazlı hacim/ret sayaçları (işyeri geçmişinden hesaplanır)
        // ------------------------------------------------------------------

        /// <summary>İşyerinin son 24 saatteki onaylı cirosu (bu işlem dahil).</summary>
        public decimal IsyeriSonGunHacmi { get; set; }

        /// <summary>
        /// İşyerinde son 1 saatte alınan ret adedi.
        /// Kaynak senaryo Kayıp/Çalıntı yanıt kodlarını ayırır; yanıt kodu saklanmadığı için
        /// burada tüm retler sayılır.
        /// </summary>
        public int IsyeriSonSaatRetAdedi { get; set; }

        /// <summary>İşyerinde son 6 saatte 2.500 TL üzeri işlem adedi (bu işlem dahil).</summary>
        public int IsyeriSonAltiSaatYuksekTutarAdedi { get; set; }

        /// <summary>İşyerinin gece penceresindeki (22:00-06:00) onaylı cirosu.</summary>
        public decimal IsyeriGeceIslemHacmi { get; set; }

        /// <summary>Bu kart bu işyerinde ilk kez mi kullanılıyor.</summary>
        public bool KartIsyerindeIlkKullanimMi { get; set; }

        /// <summary>İşyeri vergi mükellefi mi. İşyeri kaydı yoksa true kabul edilir.</summary>
        public bool IsyeriVergiMukellefiMi { get; set; } = true;

        /// <summary>İşyeri yetkilisinin doğum yılı. Bilinmiyorsa 0.</summary>
        public int IsyeriYetkiliDogumYili { get; set; }

        /// <summary>İşyerinin bulunduğu şehir. İşyeri kaydı yoksa boş.</summary>
        public string IsyeriSehri { get; set; } = string.Empty;

        /// <summary>İşyerinin son işleminden bu yana geçen gün. Geçmiş yoksa int.MaxValue.</summary>
        public int IsyeriSonIslemdenGecenGun { get; set; } = int.MaxValue;

        /// <summary>İşyerinde son 30 günde 50.000 TL ve üzeri onaylı işlem var mı.</summary>
        public bool IsyeriSon30GunYuksekTutarVarMi { get; set; }

        /// <summary>Kart numarasının ilk 6 hanesi (BIN). Kart yoksa boş.</summary>
        public string BinNo { get; set; } = string.Empty;

        // ------------------------------------------------------------------
        // BIN ve referans listelerinden türeyen alanlar
        // BIN tablosunda karşılığı olmayan kartlarda varsayılan değerlerinde kalır.
        // ------------------------------------------------------------------

        /// <summary>Kart şeması: TROY / VISA / MASTERCARD / AMEX. Bilinmiyorsa boş.</summary>
        public string KartSemasi { get; set; } = string.Empty;

        /// <summary>Kartı ihraç eden kurumun ülke kodu (ISO alpha-2). Bilinmiyorsa boş.</summary>
        public string KartUlkesi { get; set; } = string.Empty;

        /// <summary>Kart yurtdışı ihraçlı mı (ülkesi TR değil ve biliniyor).</summary>
        public bool YurtDisiKartMi { get; set; }

        /// <summary>BIN kurumun riskli listesinde mi (S41).</summary>
        public bool RiskliBinMi { get; set; }

        /// <summary>BIN yaptırım listesinde mi (S47).</summary>
        public bool YasakliBinMi { get; set; }

        /// <summary>BIN aracı kurum grubunda mı (S50).</summary>
        public bool ExpediaBinMi { get; set; }

        /// <summary>Kart ülkesi riskli ülke listesinde mi (S42).</summary>
        public bool RiskliUlkeKartiMi { get; set; }

        /// <summary>Kart ülkesi durdurulan ülke listesinde mi (S57).</summary>
        public bool DurdurulanUlkeMi { get; set; }

        /// <summary>Kart şeması durdurulan şema listesinde mi (S56).</summary>
        public bool DurdurulanSemaMi { get; set; }

        /// <summary>İşyerinin MCC'si şifresiz işleme kapalı listesinde mi (S49).</summary>
        public bool SifresizKapaliMccMi { get; set; }

        /// <summary>İşyerinin MCC'si kuyumcu/değerli maden listesinde mi (S46).</summary>
        public bool KuyumcuMccMi { get; set; }

        /// <summary>İşyerine yurtdışı kartla işlem yasak mı (S43).</summary>
        public bool IsyeriYurtDisiKartYasakMi { get; set; }

        /// <summary>İşyeri ödeme kolaylaştırıcı altı mı (S45).</summary>
        public bool IsyeriPfAltiMi { get; set; }

        // ------------------------------------------------------------------
        // Bölgesel sayaçlar (kart geçmişi + lokasyon)
        // ------------------------------------------------------------------

        /// <summary>Aynı kartın aynı lokasyonda son 24 saatteki işlem adedi (bu işlem dahil).</summary>
        public int BolgeselAyniKartAdedi { get; set; }

        /// <summary>Aynı lokasyondaki tutar dizisi azalmıyor mu (sabit ya da artan).</summary>
        public bool BolgeselTutarArtanVeyaSabitMi { get; set; }

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
        public int SonSaatFonlamaSayisi { get; set; }
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

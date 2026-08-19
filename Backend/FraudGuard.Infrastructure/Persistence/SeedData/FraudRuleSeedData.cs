using FraudGuard.Domain.Common.Enums;
using FraudGuard.Domain.Entities;

namespace FraudGuard.Infrastructure.Persistence.SeedData
{
    /// <summary>
    /// Kural kataloğunun başlangıç verisi.
    /// Tüm kurallar %100 dinamik string expression (DynamicExpresso) ile çalışır.
    /// Kod dosyası bağımlılığı yoktur; kurallar veritabanı veya API üzerinden anında yönetilebilir.
    /// </summary>
    public static class FraudRuleSeedData
    {
        public static EFraudRule[] GetRules() =>
        [
            // ----------------------------------------------------------------
            // 1-25: TCMB & Temel Fraud Kuralları (Dinamik İfadeli)
            // ----------------------------------------------------------------
            Dynamic(1, "VELOCITY", "Hız / Sıklık Kuralı",
                "input.AyniKartIslemAdedi >= 3 || input.IkiDakikadaYapilanIslemAdedi >= 3",
                "Belirli bir zaman dilimi içinde peş peşe yapılan işlemler",
                25, RuleCategoryEnum.Velocity),

            Dynamic(2, "IMPOSSIBLE_TRAVEL", "İmkansız Seyahat",
                "input.ImkansizSeyahatVarMi",
                "Fiziksel olarak mümkün olmayan süre ve mesafelerdeki işlemler",
                45, RuleCategoryEnum.Location),

            Dynamic(3, "ANOMALOUS_TIME", "Zaman ve Tutar Sapması",
                "input.GeceIslemiMi && input.Amount >= 10000",
                "Gece geç saatlerde yapılan olağandışı yüksek tutarlı işlemler",
                25, RuleCategoryEnum.Time),

            Dynamic(4, "CARD_TESTING", "Kart Deneme / Yoklama",
                "input.BinAltindaOnayliIslemAdedi >= 4 && input.IkiDakikadaYapilanIslemAdedi >= 3",
                "Kartın aktifliğini doğrulamak amacıyla yapılan küçük tutarlı denemeler",
                40, RuleCategoryEnum.Velocity),

            Dynamic(5, "BRUTE_FORCE", "Ardışık Red",
                "input.BasarisizIslemSayisi >= 3",
                "Kısa süre içinde üst üste alınan işlem reddi durumları",
                45, RuleCategoryEnum.Identity),

            Dynamic(6, "CROSS_BORDER", "Sınır Ötesi İlk İşlem",
                "input.GecmisteKullanilmayanUlkeMi || (input.YabanciUlkeMi && input.FarkliUlkeSayisi >= 2)",
                "Kart geçmişinde hiç bulunmayan bir ülkeden yapılan harcamalar",
                25, RuleCategoryEnum.Location),

            Dynamic(7, "HIGH_RISK_MCC", "Yüksek Riskli Üye İşyeri",
                "input.RiskliMccMi",
                "Kuyumcu, şans oyunları, kripto borsaları gibi riskli kategorilerden yapılan işlemler",
                20, RuleCategoryEnum.Location),

            Dynamic(8, "MAX_OUT", "Limit Boşaltma Denemesi",
                "input.KalanLimitOrani <= 0.05m && input.Amount >= 5000",
                "Kart limitinin tamamına yakınını (%95+) tek seferde harcama denemesi",
                40, RuleCategoryEnum.Amount),

            Dynamic(9, "CURRENCY_MISMATCH", "Para Birimi Sapması",
                "input.GecmisteKullanilmayanParaBirimiMi && input.Currency != \"TRY\"",
                "Müşterinin kart geçmişinde bulunmayan bir para birimiyle işlem denemesi",
                20, RuleCategoryEnum.Amount),

            Dynamic(10, "CONSECUTIVE_REFUNDS", "Ardışık İade Kuralı",
                "input.IkiSaatlikIadeIslemSayisi >= 3",
                "Kısa süre içerisinde üst üste yapılan şüpheli iade (Refund) işlemleri denemesi",
                30, RuleCategoryEnum.Velocity),

            Dynamic(11, "SMURFING", "Smurfing (Dilimleme)",
                "input.SonGunIslemSayisi >= 3 && input.SonGunIslemHacmi >= 40000",
                "Son 1 saatteki transferlerin bildirim limitini aşması",
                30, RuleCategoryEnum.Amount),

            Dynamic(12, "WALLET_CASHOUT", "Wallet Cash-Out",
                "input.CuzdanFonlamaSonrasiNakitCikisVarMi || (input.SonIslemdenGecenDakika <= 15 && input.Amount >= 20000)",
                "Cüzdan fonlanmasından hemen sonra EFT ile çıkış yapılması",
                40, RuleCategoryEnum.Velocity),

            // Kaynak kart bilgisi işlem kaydında tutulmadığı için "farklı kart" boyutu
            // ölçülemiyor; kural fonlama sıklığı üzerinden çalışır.
            Dynamic(13, "MULTI_SOURCE_FUNDING", "Kısa Sürede Çoklu Fonlama",
                "input.SonSaatFonlamaSayisi >= 3",
                "Hesaba son 1 saat içinde 3 veya daha fazla bakiye yükleme yapılması",
                30, RuleCategoryEnum.Velocity),

            Dynamic(14, "CROSS_BORDER_TRANSFER", "Sınır Ötesi Transfer",
                "input.YabanciUlkeMi && input.Amount >= 25000",
                "İlk defa yurt dışı IBAN'a yüksek tutarlı EFT yollanması",
                30, RuleCategoryEnum.Location),

            Dynamic(15, "ACCOUNT_DRAIN", "Hesap Boşaltma Denemesi",
                "input.BakiyeCekimOrani >= 0.95m && input.Amount >= 5000",
                "Tek işlemde hesap bakiyesinin %95 ve üzerini çekme denemesi",
                45, RuleCategoryEnum.Amount),

            Dynamic(16, "NEW_BENEFICIARY_TRANSFER", "Yeni Alıcı Transfer Anormalliği",
                "input.YeniAliciMi && input.Amount >= 15000",
                "Yeni eklenen alıcıya ilk 5 dakikada yüksek transfer yapılması",
                30, RuleCategoryEnum.Identity),

            Dynamic(17, "SUSPICIOUS_DESCRIPTION", "Şüpheli İşlem Açıklaması",
                "input.YasakliKelimeIceriyorMu",
                "Açıklamada bahis, kripto vb. yasaklı kelimelerin bulunması",
                20, RuleCategoryEnum.Identity),

            // Kesin kural: alıcı hesabın bloke olması deterministik bir yaptırım sinyalidir,
            // güven geçmişi tarafından bastırılmamalıdır.
            Dynamic(18, "HIGH_RISK_RECEIVER", "Şüpheli Alıcı/Katır Hesap",
                "input.RiskliAliciMi",
                "Gönderilen IBAN'ın sistemde kara listede olması",
                45, RuleCategoryEnum.Identity, isCritical: true),

            Dynamic(19, "MULTI_SENDER_TO_SINGLE_RECEIVER", "Tek Alıcıya Çoklu Gönderim",
                "input.Son1SaatFarkliGondericiSayisi >= 3",
                "Aynı alıcıya kısa sürede farklı kişilerden para transferi",
                30, RuleCategoryEnum.Velocity),

            Dynamic(20, "RECEIVER_BALANCE_ANOMALY", "Katır Hesap Bakiye Sapması",
                "input.KatirHesapBakiyeAnormalligiVarMi || (input.SonGunIslemHacmi >= 50000 && input.SonIslemdenGecenDakika <= 60)",
                "Pasif hesaba ani bakiye gelip 1 saatte nakit çekilmeye çalışılması",
                30, RuleCategoryEnum.Velocity),

            Dynamic(22, "HIGH_VALUE_REFUND_VOID", "Yüksek Tutarlı İade Kuralı",
                "input.Amount >= 10000",
                "Tek seferde 10.000 TL ve üzerinde İade (Refund) işlemi yapılması",
                25, RuleCategoryEnum.Amount),

            Dynamic(23, "DEPOSIT_AND_RUN", "Yatır ve Kaç Kuralı",
                "input.NakitYatirmaSonrasiHarcanmaOrani >= 0.90m",
                "Hesaba ATM'den para yatırıldıktan hemen sonra tutarın %90'ından fazlasının harcanmak istenmesi",
                35, RuleCategoryEnum.Velocity),

            Dynamic(24, "DEPOSIT_LIMIT_AVOIDANCE", "Yapılandırılmış Aklama",
                "input.SonGunAtmNakitYatirmaSayisi >= 3 && input.SonGunAtmNakitYatirmaHacmi >= 40000",
                "Son 24 saatte 3 veya daha fazla farklı ATM'den toplamda 40.000 TL ve üzeri para yatırma denemesi",
                35, RuleCategoryEnum.Amount),

            Dynamic(25, "ANOMALOUS_DEPOSIT_TIME", "Gece Yarısı Nakit Akışı",
                "input.GeceIslemiMi && input.GeceNakitYatirmaHacmi >= 10000",
                "Gece geç saatte (23:00-06:00) ATM'den 10.000 TL ve üzeri nakit para yatırılması",
                20, RuleCategoryEnum.Time),

            // ----------------------------------------------------------------
            // 26+: İlave Dinamik Senaryolar (Standart Tanımlı)
            // ----------------------------------------------------------------
            Dynamic(26, "RAPID_TXN_VELOCITY_2MIN", "2 Dakikada Hızlı İşlem Sıklığı",
                "input.IkiDakikadaYapilanIslemAdedi >= 3",
                "Aynı kartla 2 dakika içinde 3 veya daha fazla işlem denenmesi",
                35, RuleCategoryEnum.Velocity),

            Dynamic(27, "HOURLY_SAME_CARD_VELOCITY", "1 Saatte Aynı Kartla Yoğun İşlem",
                "input.AyniKartIslemAdedi >= 3",
                "Aynı kartla 1 saat içinde 3 veya daha fazla işlem denenmesi",
                30, RuleCategoryEnum.Velocity),

            Dynamic(28, "INTENSE_FAILED_ATTEMPTS", "Yoğun Başarısız İşlem Denemeleri",
                "input.BasarisizIslemSayisi >= 3",
                "Son 24 saatte aynı kartla 3 veya daha fazla başarısız işlem denemesi",
                40, RuleCategoryEnum.Velocity),

            Dynamic(29, "NIGHT_HIGH_VALUE", "Gece Saatlerinde Yüksek Tutar",
                "input.GeceIslemiMi && input.Amount >= 10000",
                "22:00-06:00 arasında 10.000 TL ve üzeri işlem",
                25, RuleCategoryEnum.Time),

            Dynamic(30, "PROBING_THEN_HIGH_VALUE", "Küçük Denemeler Sonrası Ani Yüksek Tutar",
                "input.BinAltindaOnayliIslemAdedi >= 5 && input.Amount >= 50000",
                "Son 1 saatte 1.000 TL altı 5+ onaylı işlem ardından 50.000 TL ve üzeri işlem",
                45, RuleCategoryEnum.Amount),

            Dynamic(31, "DAILY_LIMIT_EXCEEDED", "Günlük Hacim ve Adet Aşımı",
                "input.SonGunIslemHacmi >= 50000 && input.SonGunIslemSayisi > 5",
                "Son 24 saatte toplam hacim 50.000 TL'yi ve işlem adedi 5'i aşması",
                20, RuleCategoryEnum.Amount),

            Dynamic(32, "REFUND_EXCEEDS_SALES", "İade Tutarı Satış Tutarını Aştı",
                "input.ToplamIadeTutari > 0 && input.ToplamIadeTutari > input.ToplamSatisTutar",
                "Son 24 saatteki iade tutarının aynı dönemdeki satış tutarını aşması",
                35, RuleCategoryEnum.Amount),

            Dynamic(33, "CONSECUTIVE_REFUNDS_2HOURS", "2 Saatte Yoğun İade Denemesi",
                "input.IkiSaatlikIadeIslemSayisi > 5",
                "Son 2 saat içinde 5'ten fazla iade işlemi yapılması",
                30, RuleCategoryEnum.Velocity),

            Dynamic(34, "AVERAGE_MULTIPLIER_SURGE", "Ortalamanın 4 Katı Anormal Tutar",
                "input.OrtalamaIslemTutari > 0 && input.Amount > 4 * input.OrtalamaIslemTutari",
                "İşlem tutarının son 24 saatlik ortalamanın 4 katını aşması",
                25, RuleCategoryEnum.Amount),

            Dynamic(35, "MULTI_COUNTRY_ACTIVITY", "Çoklu Ülke Eşzamanlı Aktivitesi",
                "input.FarkliUlkeSayisi >= 3",
                "Son 24 saatte aynı kartla 3 veya daha fazla farklı ülkeden işlem",
                30, RuleCategoryEnum.Location),

            // ----------------------------------------------------------------
            // İşyeri bazlı kurallar
            // Yalnızca istekte MerchantId gönderilen işlemlerde değerlendirilir;
            // işyeri seçilmemişse sayaçlar varsayılanda kalır ve kural tetiklenmez.
            // ----------------------------------------------------------------
            MerchantRule(36, "MERCHANT_MULTI_CARD_VELOCITY", "İşyerinde 1 Saatte Çoklu Kart Denemesi",
                "input.FarkliKartSayisi >= 3",
                "Aynı işyerinde 1 saat içinde 3 veya daha fazla farklı kartla işlem denenmesi",
                30, RuleCategoryEnum.Velocity),

            MerchantRule(37, "NEW_MERCHANT_HIGH_TURNOVER", "Yeni İşyeri Ani Yüksek Ciro",
                "input.IsyeriYasiGun <= 30 && input.SonGunIslemHacmi > 200000",
                "POS tahsisinden sonraki 30 gün içinde 24 saatlik cironun 200.000 TL'yi aşması",
                35, RuleCategoryEnum.Amount),

            MerchantRule(38, "MERCHANT_CARD_SPRAY", "Kartın Çok Sayıda İşyerine Yayılması",
                "input.FarkliIsyeriSayisi >= 5",
                "Aynı kartın son 24 saatte 5 veya daha fazla farklı işyerinde kullanılması",
                25, RuleCategoryEnum.Velocity),

            // ----------------------------------------------------------------
            // SENARYO İHTİYAÇLARI listesinden eklenenler (Dalga 1-2).
            // Kaynak ifadeler SQL olarak yazılmıştı; ağır iş enricher sayaçlarına
            // taşınıp ifadeler eşik karşılaştırmasına indirgenmiştir.
            // ----------------------------------------------------------------

            // S14 — kaynak: 1M VE ÜSTÜ İŞLEM
            Dynamic(39, "AMOUNT_OVER_1M", "1.000.000 TL Üzeri İşlem",
                "input.Amount > 1000000",
                "Tek işlemde 1.000.000 TL üzeri tutar",
                30, RuleCategoryEnum.Amount),

            // S20 — kaynak: 500K VE ÜSTÜ İŞLEM
            Dynamic(40, "AMOUNT_OVER_500K", "500.000 TL ve Üzeri İşlem",
                "input.Amount >= 500000 && input.Amount <= 1000000",
                "Tek işlemde 500.000 TL ve üzeri tutar",
                20, RuleCategoryEnum.Amount),

            // S49 — kaynak: ŞİFRESİZ İŞLEMLERİNE RET VERİLEN MCC
            Dynamic(41, "PINLESS_BLOCKED_MCC", "Şifresiz İşleme Kapalı MCC",
                "input.Auth.PinExist == false && (input.MccKodu == \"5944\" || input.MccKodu == \"5094\" || input.MccKodu == \"7995\" || input.MccKodu == \"6051\")",
                "Şifresiz işleme kapalı MCC'de (kuyumcu, şans oyunu, kripto) PIN'siz işlem",
                40, RuleCategoryEnum.Identity),

            // S52 — kaynak: AYNI KART AYNI TUTARDA TEMASSIZ YURTDIŞI İŞLEMLERE RET
            Dynamic(42, "CONTACTLESS_FOREIGN_REPEAT", "Temassız Yurtdışı Tekrarlı Tutar",
                "input.Auth.Contactless == true && input.YabanciUlkeMi && input.AyniKartAyniTutarAdedi >= 2",
                "Aynı kartla yurtdışında aynı tutarda tekrarlayan temassız işlem",
                40, RuleCategoryEnum.Location),

            // S53 — kaynak: BİR GÜNDE AYNI İŞYERİNDE 2500 TL VE ÜZERİ OFFLINE İŞLEMLER
            // Modelde ayrı bir offline bayrağı yok; gecikmiş otorizasyon (DeferredAuth) karşılık alındı.
            Dynamic(43, "OFFLINE_HIGH_VALUE", "Offline Yüksek Tutarlı İşlem",
                "input.Auth.DeferredAuth == true && input.Amount >= 2500",
                "Gecikmiş otorizasyonla 2.500 TL ve üzeri işlem",
                35, RuleCategoryEnum.Amount),

            // S13 — kaynak: BÖLGE 24 SAAT AYNI KART ARTTIRARAK/AYNI TUTAR
            Dynamic(44, "REGIONAL_ESCALATING_AMOUNT", "Bölgesel Artan Tutarlı Tekrar",
                "input.BolgeselAyniKartAdedi >= 3 && input.BolgeselTutarArtanVeyaSabitMi",
                "Aynı kartla aynı bölgede 24 saatte 3. işlem; tutarlar sabit ya da artıyor",
                40, RuleCategoryEnum.Velocity),

            // S58 — kaynak: KARTIN İLK KEZ KULLANIMI
            Dynamic(45, "FIRST_USE_AT_MERCHANT", "Kartın İşyerinde İlk Kullanımı",
                "input.KartIsyerindeIlkKullanimMi && input.Amount >= 10000",
                "Bu kart bu işyerinde ilk kez kullanılıyor ve tutar 10.000 TL üzeri",
                15, RuleCategoryEnum.Identity),

            // S4 — kaynak: GÜNLÜK YÜKSEK İŞLEM HACMİ (MCC 9399/9311/8062 hariç)
            MerchantRule(46, "MERCHANT_DAILY_VOLUME", "İşyeri Günlük Yüksek Hacim",
                "input.IsyeriSonGunHacmi >= 50000 && input.MccKodu != \"9399\" && input.MccKodu != \"9311\" && input.MccKodu != \"8062\"",
                "İşyerinin 24 saatlik cirosu 50.000 TL'yi aştı (kamu/vergi MCC'leri hariç)",
                20, RuleCategoryEnum.Amount),

            // S7 — kaynak: 3 VE ÜZERİ SAYIDA K/Ç YANITI ALAN İŞ YERLERİ
            MerchantRule(47, "MERCHANT_DECLINE_BURST", "İşyerinde Yoğun Ret",
                "input.IsyeriSonSaatRetAdedi >= 3",
                "İşyerinde son 1 saatte 3 veya daha fazla ret alındı",
                30, RuleCategoryEnum.Velocity),

            // S51 — kaynak: MOBİL FLAG YES / 6 SAATTE 2500 ÜSTÜ 10+ İŞLEM
            MerchantRule(48, "MERCHANT_MOBILE_HIGH_VALUE_BURST", "İşyerinde Mobil Yüksek Tutar Yoğunluğu",
                "input.Auth.MobileTransaction == true && input.IsyeriSonAltiSaatYuksekTutarAdedi >= 10",
                "Mobil işlemde, işyerinde son 6 saatte 2.500 TL üzeri 10+ işlem",
                40, RuleCategoryEnum.Velocity),

            // S3 — kaynak: GECE ISLEMLERI (hafta sonu %30 artış)
            MerchantRule(49, "MERCHANT_WEEKEND_NIGHT_SURGE", "Hafta Sonu Gece Ciro Sıçraması",
                "input.HaftaSonuMu && input.GeceIslemiMi && input.IsyeriGeceIslemHacmi > 0 && input.Amount > 1.30m * input.OrtalamaIslemTutari",
                "Hafta sonu gece penceresinde ortalamanın %30 üzerinde işlem",
                10, RuleCategoryEnum.Time),

            // ----------------------------------------------------------------
            // BIN / referans listesi tabanli senaryolar (S41-S47, S50, S56, S57)
            // ----------------------------------------------------------------

            Dynamic(50, "RISKY_BIN", "Riskli BIN",
                "input.RiskliBinMi",
                "Kart BIN kodu kurumun riskli listesinde",
                45, RuleCategoryEnum.Identity),

            Dynamic(51, "SANCTIONED_BIN", "Yaptirim Listesindeki BIN",
                "input.YasakliBinMi",
                "Kart BIN kodu yaptirim (OFAC) listesinde",
                60, RuleCategoryEnum.Identity, isCritical: true),

            Dynamic(52, "RISKY_COUNTRY_CARD", "Riskli Ulke Karti",
                "input.RiskliUlkeKartiMi",
                "Kart riskli ulke ihracli",
                35, RuleCategoryEnum.Location),

            Dynamic(53, "BLOCKED_COUNTRY_CARD", "Durdurulan Ulke Karti",
                "input.DurdurulanUlkeMi",
                "Kartin ihrac ulkesi islem durdurma listesinde",
                60, RuleCategoryEnum.Location, isCritical: true),

            Dynamic(54, "BLOCKED_SCHEME", "Durdurulan Kart Semasi",
                "input.DurdurulanSemaMi",
                "Kart semasi islem durdurma listesinde",
                60, RuleCategoryEnum.Identity, isCritical: true),

            Dynamic(55, "EXPEDIA_BIN", "Araci Kurum BIN Islemi",
                "input.ExpediaBinMi",
                "Islem araci kurum BIN grubuyla yapildi",
                30, RuleCategoryEnum.Identity),

            Dynamic(56, "FOREIGN_CARD_TRANSACTION", "Yurtdisi Kart Islemi",
                "input.YurtDisiKartMi",
                "Yurtdisi ihracli kartla islem",
                10, RuleCategoryEnum.Location),

            MerchantRule(57, "FOREIGN_CARD_AT_BLOCKED_MERCHANT", "Yurtdisi Karta Kapali Isyeri",
                "input.YurtDisiKartMi && input.IsyeriYurtDisiKartYasakMi",
                "Isyeri yurtdisi kartlara kapali oldugu halde yurtdisi kartla islem",
                50, RuleCategoryEnum.Location),

            MerchantRule(58, "CROSSBORDER_NON_TAXPAYER", "Sinir Otesi / Vergi Mukellefi Degil",
                "input.YurtDisiKartMi && input.IsyeriVergiMukellefiMi == false",
                "Sinir otesi islemde isyeri vergi mukellefi degil",
                50, RuleCategoryEnum.Identity),

            MerchantRule(59, "PF_SUB_CROSSBORDER_NON_TROY", "PF Alti Sinir Otesi TROY Disi",
                "input.IsyeriPfAltiMi && input.YurtDisiKartMi && input.KartSemasi != \"TROY\" && input.KartSemasi != \"\"",
                "Odeme kolaylastirici alti isyerinde TROY disi sinir otesi islem",
                45, RuleCategoryEnum.Location),

            Dynamic(60, "RISKY_COUNTRY_JEWELRY", "Riskli Ulke Karti ile Kuyumcu Islemi",
                "input.RiskliUlkeKartiMi && input.KuyumcuMccMi",
                "Riskli ulke ihracli kartla kuyumcu/degerli maden islemi",
                55, RuleCategoryEnum.Location, isCritical: true),

            // ----------------------------------------------------------------
            // Isyeri master ve Auth tabanli senaryolar (S8, S19, S25, S48, S54, S55)
            // ----------------------------------------------------------------

            MerchantRule(61, "MERCHANT_DORMANT_REACTIVATION", "Uzun Sessizlik Sonrasi Ilk Islem",
                "input.IsyeriSonIslemdenGecenGun >= 30 && input.IsyeriSonIslemdenGecenGun < 2147483647",
                "Isyeri 30 gunden uzun suredir islem yapmamisken yeniden islem yapiyor",
                15, RuleCategoryEnum.Time),

            MerchantRule(62, "MERCHANT_FIRST_HIGH_VALUE", "30 Gun Sonra Ilk Yuksek Tutar",
                "input.IsyeriSon30GunYuksekTutarVarMi == false && input.Amount >= 50000",
                "Isyeri son 30 gunde hic 50.000 TL uzeri islem gormemisken ilk kez goruyor",
                35, RuleCategoryEnum.Amount),

            MerchantRule(63, "MERCHANT_POST_DECLINE_ACTIVITY", "Ret Sonrasi Isyeri Aktivitesi",
                "input.IsyeriSonSaatRetAdedi >= 1 && input.Amount >= 1000",
                "Isyerinde son 1 saatte ret alinmisken devam eden islem",
                20, RuleCategoryEnum.Velocity),

            MerchantRule(64, "NEW_MERCHANT_PINLESS_PHYSICAL", "Yeni Isyerinde PINsiz Fiziksel Islem",
                "input.IsyeriYasiGun <= 30 && input.Auth.PinExist == false && input.Auth.CardPresent == true",
                "POS tahsisinden sonraki 30 gun icinde PINsiz fiziksel islem",
                45, RuleCategoryEnum.Identity),

            MerchantRule(65, "MERCHANT_YOUNG_OWNER", "Yasca Kucuk Isyeri Yetkilisi",
                "input.IsyeriYetkiliDogumYili >= 2006",
                "Isyeri yetkilisi 2006 ve sonrasi dogumlu",
                25, RuleCategoryEnum.Identity),

            MerchantRule(66, "MERCHANT_WATCHLIST_CITY", "Izleme Listesindeki Il",
                "input.IsyeriSehri == \"Adana\" || input.IsyeriSehri == \"Mersin\" || input.IsyeriSehri == \"Gaziantep\"",
                "Isyeri adresi izleme listesindeki illerden birinde. Yalnizca izleme amaclidir, tek basina ret gerekcesi degildir",
                10, RuleCategoryEnum.Location),

            // ----------------------------------------------------------------
            // Veri kaynagi bulunmadigi icin PASIF eklenen senaryolar.
            // Katalogda gorunurler; engelleri kalktiginda ifade yazilip aktif edilir.
            // ----------------------------------------------------------------

            Blocked(67, "WEEKLY_ANOMALY_TRACKING", "Haftalik Anomali Takibi",
                "Haftalik islem adedi/tutari, ret orani ve gece payindaki sicramalar",
                "haftalik baseline istatistigi", 25, RuleCategoryEnum.Velocity),

            Blocked(68, "DECLINE_RATE_SURGE", "Ret Oraninda Ani Artis",
                "Ret oraninin haftalik bazda 20 puan uzeri artmasi",
                "haftalik ret orani istatistigi", 20, RuleCategoryEnum.Velocity),

            Blocked(69, "FLAT_AMOUNT_RATIO", "Aylik Duz Tutarli Islem Yogunlugu",
                "Aylik islem adedinin yuzde 25inin tekrarli duz tutarlardan olusmasi",
                "30 gunluk tutar dagilimi istatistigi", 35, RuleCategoryEnum.Amount),

            Blocked(70, "REPEATED_FIXED_AMOUNT", "Tekrar Eden Sabit Tutarli Islem",
                "Son 30 gunde tek bir tutarin islem adedinin yuzde 25ini olusturmasi",
                "30 gunluk tutar dagilimi istatistigi", 35, RuleCategoryEnum.Amount),

            Blocked(71, "DAILY_ANOMALY_TRACKING", "Gunluk Anomali Takibi",
                "Bir onceki gune gore adet veya tutarda 2 kat artis",
                "gunluk baseline istatistigi", 20, RuleCategoryEnum.Velocity),

            Blocked(72, "SECTOR_TURNOVER_EXCESS", "Sektor Ciro Ortalamasini Asma",
                "MCC bazli sektor gunluk ciro ortalamasinin yuzde 30 uzeri",
                "sektor (MCC) baseline istatistigi", 20, RuleCategoryEnum.Amount),

            Blocked(73, "SECTOR_COUNT_EXCESS", "Sektor Islem Adedi Ortalamasini Asma",
                "MCC bazli sektor gunluk adet ortalamasinin yuzde 30 uzeri",
                "sektor (MCC) baseline istatistigi", 20, RuleCategoryEnum.Velocity),

            Blocked(74, "VOLUME_VS_WEEKLY_BASELINE", "24 Saatlik Hacim Anomalisi",
                "24 saatlik hacmin onceki 7 gunun gunluk ortalamasini asmasi",
                "7 gunluk hacim baseline istatistigi", 25, RuleCategoryEnum.Amount),

            Blocked(75, "NIGHT_DEVIATION_TARGETED", "Gece Islem Sapmasi (Hedef Isyerleri)",
                "Hedef isyerlerinde gece adet/tutarin 30 gecelik ortalamayi yuzde 20 asmasi",
                "30 gecelik baseline istatistigi", 25, RuleCategoryEnum.Time),

            Blocked(76, "NIGHT_COUNT_INCREASE", "Gece Islem Adedi Artisi",
                "Gece adedinin 30 gunluk gece ortalamasini yuzde 15 asmasi",
                "30 gecelik baseline istatistigi", 20, RuleCategoryEnum.Time),

            Blocked(77, "NIGHT_AMOUNT_INCREASE", "Gece Islem Tutari Artisi",
                "Gece tutarinin 30 gunluk gece ortalamasini yuzde 15 asmasi",
                "30 gecelik baseline istatistigi", 20, RuleCategoryEnum.Time),

            Blocked(78, "DAILY_AMOUNT_SURGE", "Gunluk Tutarda Ani Artis",
                "Dunku tutar 20.000 TL uzerindeyken bugunku tutarin 4 katina ulasmasi",
                "gunluk baseline istatistigi", 25, RuleCategoryEnum.Amount),

            Blocked(79, "WEEKLY_COUNT_SURGE", "Haftalik Adette Ani Artis",
                "Bu haftaki adedin onceki haftanin 2 katina ulasmasi",
                "haftalik baseline istatistigi", 20, RuleCategoryEnum.Velocity),

            Blocked(80, "SAME_CARD_MULTIPLE_PHONES", "Ayni Kart Farkli Telefon",
                "Ayni kartin 24 saatte en az 2 farkli hamil telefonuyla kullanilmasi",
                "islem bazli kart hamili telefon bilgisi", 30, RuleCategoryEnum.Identity, RuleTargetEnum.Card),

            Blocked(81, "SAME_CARD_MULTIPLE_EMAILS", "Ayni Kart Farkli E-posta",
                "Ayni kartin 24 saatte en az 2 farkli hamil e-postasiyla kullanilmasi",
                "islem bazli kart hamili e-posta bilgisi", 30, RuleCategoryEnum.Identity, RuleTargetEnum.Card)
        ];

        /// <param name="isCritical">
        /// Deterministik yaptırım kuralları için true. Bu kuralların puanı güven indiriminden
        /// muaf tutulur; sezgisel kurallarda kullanılmamalıdır.
        /// </param>
        private static EFraudRule Dynamic(
            int id, string code, string name, string expression, string description,
            int score, RuleCategoryEnum category, bool isCritical = false) =>
            new()
            {
                RuleId = id,
                RuleCode = code,
                RuleName = name,
                Description = description,
                Expression = expression,
                Score = score,
                Target = RuleTargetEnum.Card,
                Category = category,
                IsCritical = isCritical,
                IsActive = true
            };

        /// <summary>
        /// İşyeri bazlı kural. Puanı kart havuzuna değil işyeri havuzuna yazılır.
        /// EMerchant verisi eklendiğinden beri bu kurallar aktif çalışır.
        /// </summary>
        /// <summary>
        /// Kaynak listede tanimli olan ancak gerekli veri kaynagi henuz bulunmadigi icin
        /// <b>pasif</b> eklenen senaryo. Katalogda gorunur, degerlendirmeye girmez.
        /// Engeli kalktiginda ifadesi yazilip aktif edilir.
        /// </summary>
        private static EFraudRule Blocked(
            int id, string code, string name, string description, string blocker,
            int score, RuleCategoryEnum category, RuleTargetEnum target = RuleTargetEnum.Merchant) =>
            new()
            {
                RuleId = id,
                RuleCode = code,
                RuleName = name,
                Description = $"{description} [ENGEL: {blocker}]",
                Expression = "false",
                Score = score,
                Target = target,
                Category = category,
                IsActive = false
            };

        private static EFraudRule MerchantRule(
            int id, string code, string name, string expression, string description,
            int score, RuleCategoryEnum category) =>
            new()
            {
                RuleId = id,
                RuleCode = code,
                RuleName = name,
                Description = description,
                Expression = expression,
                Score = score,
                Target = RuleTargetEnum.Merchant,
                Category = category,
                IsActive = true
            };
    }
}

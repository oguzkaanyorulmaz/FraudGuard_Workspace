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

            Dynamic(13, "MULTI_SOURCE_FUNDING", "Çoklu Kaynakla Fonlama",
                "input.KisaSuredeFarkliKartlaFonlamaSayisi >= 3",
                "Aynı cüzdana kısa sürede farklı kartlarla bakiye yüklenmesi",
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

            Dynamic(18, "HIGH_RISK_RECEIVER", "Şüpheli Alıcı/Katır Hesap",
                "input.RiskliAliciMi",
                "Gönderilen IBAN'ın sistemde kara listede olması",
                45, RuleCategoryEnum.Identity),

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
            // İşyeri bazlı şablonlar
            // ----------------------------------------------------------------
            MerchantTemplate(36, "MERCHANT_MULTI_CARD_VELOCITY", "İşyerinde 1 Saatte Çoklu Kart Denemesi",
                "input.FarkliKartSayisi >= 3 || input.AyniKartIslemAdedi >= 3",
                "Aynı işyerinde 1 saat içinde 3 farklı kart veya aynı kartla 3 işlem denenmesi",
                30, RuleCategoryEnum.Velocity),

            MerchantTemplate(37, "NEW_MERCHANT_HIGH_TURNOVER", "Yeni İşyeri Ani Yüksek Ciro",
                "input.PosTahsisTarihi != null && input.SonGunIslemHacmi > 200000",
                "POS tahsisinden sonraki 30 gün içinde 24 saatlik cironun 200.000 TL'yi aşması",
                35, RuleCategoryEnum.Amount)
        ];

        private static EFraudRule Dynamic(
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
                Target = RuleTargetEnum.Card,
                Category = category,
                IsActive = true
            };

        private static EFraudRule MerchantTemplate(
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
                IsActive = false
            };
    }
}

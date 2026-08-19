using FraudGuard.Domain.Common.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using FraudGuard.Domain.Common.Enums;
using FraudGuard.Domain.DomainObjects.FraudEvaluation;
using FraudGuard.Domain.DomainObjects.TransactionProcessing;
using FraudGuard.Domain.Entities;
using FraudGuard.Domain.Interfaces.Entities;

namespace FraudGuard.Domain.Services.RuleEngine
{
    /// <summary>
    /// İşlem geçmişinden sayaçları hesaplayıp <see cref="ProcessTransactionInput"/> üzerine yazar.
    /// <para>
    /// Dinamik kural ifadeleri yalnızca input üzerindeki alanlara erişebildiği için, ifadelerin
    /// anlamlı çalışabilmesi bu zenginleştirmeye bağlıdır. Sayaçlar <b>değerlendirilen işlem dahil</b>
    /// hesaplanır: "aynı kartla 3 işlem" koşulu, geçmişteki 2 işlem + mevcut işlem ile sağlanır.
    /// </para>
    /// <para>
    /// Kapsam iki ayrı geçmiştir: karta/IBAN'a ait son 24 saat ve — işlem bir üye işyerine
    /// bağlıysa — o işyerine ait son 24 saat. İşyeri geçmişi verilmezse işyeri bazlı sayaçlar
    /// varsayılan değerlerinde kalır ve onları kullanan kurallar tetiklenmez.
    /// </para>
    /// </summary>
    public static class TransactionInputEnricher
    {
        private const decimal SmallTransactionCeiling = 1000m;

        /// <summary>S51/S53 eşiği: "yüksek tutarlı" sayılan alt sınır.</summary>
        private const decimal HighValueTransactionFloor = 2500m;

        /// <summary>S25 eşiği: işyeri ölçeğinde "yüksek tutarlı" sayılan alt sınır.</summary>
        private const decimal MerchantHighValueFloor = 50000m;
        private const int NightStartHour = 22;
        private const int NightEndHour = 6;

        /// <summary>
        /// <see cref="ProcessTransactionInput"/> üzerinde tanımlı olup bu enricher'ın
        /// <b>doldurmadığı</b> alanlar ve nedenleri.
        /// <para>
        /// Böyle bir alanı kullanan kural derlenir, kaydedilir ve katalogda aktif görünür —
        /// ama alan varsayılan değerinde kaldığı için hiç tetiklenmez ve
        /// <c>ruleFailures</c>'a da düşmez. Yani sessizce ölüdür. Kural yazma arayüzü bu listeyi
        /// okuyup yazarı önden uyarır.
        /// </para>
        /// <para>
        /// Liste bilinçli olarak enricher'ın yanında durur: bir alanın dolup dolmadığı bu sınıfın
        /// davranışıdır, başka bir katmanın bilgisi değil. Buraya yeni bir sayaç eklendiğinde
        /// ilgili satır <b>silinmelidir</b>; hesaplanmayan yeni bir alan eklendiğinde ise
        /// buraya <b>eklenmelidir</b>.
        /// </para>
        /// </summary>
        public static readonly IReadOnlyDictionary<string, string> UnpopulatedFields =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["KatirHesapBakiyeAnormalligiVarMi"] =
                    "Alıcı hesabın bakiye hareketi izlenmiyor; bu gösterge hesaplanmıyor."
            };

        public static ProcessTransactionInput Enrich(
            ProcessTransactionInput input,
            IReadOnlyList<ITransaction> history,
            decimal cardLimit = 0m,
            decimal cardBalance = 0m,
            DateTime? evaluatedAt = null,
            EMerchant? merchant = null,
            IReadOnlyList<ITransaction>? merchantHistory = null,
            int cardId = 0,
            bool isCreditCard = true,
            IReadOnlyList<ITransaction>? receiverHistory = null,
            bool isReceiverBlocked = false,
            ReferenceDataContext? referenceData = null)
        {
            var now = evaluatedAt ?? DateTime.Now;

            ApplyTimeFields(input, now);

            var window24H = history
                .Where(t => t.TransactionDate <= now && (now - t.TransactionDate) <= TimeSpan.FromHours(24))
                .ToList();

            ApplyVolumeCounters(input, window24H, now);
            ApplyVelocityCounters(input, window24H, now);
            ApplyRefundCounters(input, window24H, now);
            ApplyDiversityCounters(input, window24H);
            ApplyRegionalCounters(input, window24H);
            ApplyLimitAndBalance(input, cardLimit, cardBalance);
            ApplySecurityAndPatternIndicators(input, window24H, now);
            ApplyMerchantFields(input, merchant, merchantHistory, window24H, now, cardId, isCreditCard);
            ApplyTransferIndicators(input, window24H, receiverHistory, isReceiverBlocked, now);
            ApplyReferenceDataIndicators(input, referenceData);

            return input;
        }

        /// <summary>
        /// Alıcı tarafına bakan transfer göstergelerini yazar.
        /// <para>
        /// <see cref="ProcessTransactionInput.YeniAliciMi"/> ve
        /// <see cref="ProcessTransactionInput.CuzdanFonlamaSonrasiNakitCikisVarMi"/> gönderenin kendi
        /// geçmişinden hesaplanabilir. Buna karşılık <see cref="ProcessTransactionInput.RiskliAliciMi"/>
        /// ve <see cref="ProcessTransactionInput.Son1SaatFarkliGondericiSayisi"/> alıcı hesabına ait
        /// veri gerektirir; bunlar orkestratör tarafından hazır geçilir. Enricher böylece saf kalır.
        /// </para>
        /// </summary>
        private static void ApplyTransferIndicators(
            ProcessTransactionInput input,
            IReadOnlyList<ITransaction> window,
            IReadOnlyList<ITransaction>? receiverHistory,
            bool isReceiverBlocked,
            DateTime now)
        {
            // Sistemde bloke edilmiş bir hesaba gönderim: katır hesap sinyali.
            input.RiskliAliciMi = isReceiverBlocked;

            if (receiverHistory is not null)
            {
                input.Son1SaatFarkliGondericiSayisi = receiverHistory
                    .Where(t => (now - t.TransactionDate) <= TimeSpan.FromHours(1))
                    .Select(t => t.SenderIBAN)
                    .Where(iban => !string.IsNullOrWhiteSpace(iban))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count();
            }

            bool isTransfer = input.PaymentType is PaymentTypeEnum.EFT or PaymentTypeEnum.BankTransfer;
            if (!isTransfer)
                return;

            // Alıcıya daha önce onaylanmış transfer yoksa "yeni alıcı" sayılır.
            input.YeniAliciMi =
                !string.IsNullOrWhiteSpace(input.ReceiverIBAN) &&
                !window.Any(t => IsApproved(t) &&
                                 string.Equals(t.ReceiverIBAN, input.ReceiverIBAN, StringComparison.OrdinalIgnoreCase));

            // Hesap son 15 dakika içinde fonlanıp hemen çıkış yapılıyor mu.
            input.CuzdanFonlamaSonrasiNakitCikisVarMi = window.Any(t =>
                IsApproved(t) &&
                t.TransactionTypeId == (int)TransactionTypeEnum.Deposit &&
                (now - t.TransactionDate) <= TimeSpan.FromMinutes(15));
        }

        /// <summary>
        /// İşyeri master verisinden gelen alanları ve işyeri bazlı sayaçları yazar.
        /// <para>
        /// İki sayaç farklı yönlere bakar ve karıştırılmamalıdır:
        /// <see cref="ProcessTransactionInput.FarkliKartSayisi"/> <b>işyeri</b> geçmişinden
        /// (bu POS'ta kaç farklı kart), <see cref="ProcessTransactionInput.FarkliIsyeriSayisi"/>
        /// ise <b>kart</b> geçmişinden (bu kart kaç farklı işyerinde) hesaplanır.
        /// </para>
        /// </summary>
        private static void ApplyMerchantFields(
            ProcessTransactionInput input,
            EMerchant? merchant,
            IReadOnlyList<ITransaction>? merchantHistory,
            IReadOnlyList<ITransaction> cardWindow24H,
            DateTime now,
            int cardId,
            bool isCreditCard)
        {
            if (merchant is not null)
            {
                input.MerchantId = merchant.MerchantId;
                input.MccKodu = merchant.MccCode;
                input.PosTahsisTarihi = merchant.PosAssignmentDate;
                input.IsyeriYasiGun = (int)Math.Max(0, (now - merchant.PosAssignmentDate).TotalDays);
                input.IsyeriVergiMukellefiMi = merchant.IsTaxpayer;
                input.IsyeriSehri = merchant.City ?? string.Empty;
                input.IsyeriYetkiliDogumYili = merchant.OwnerBirthDate?.Year ?? 0;
                input.IsyeriYurtDisiKartYasakMi = merchant.ForeignCardsBlocked;
                input.IsyeriPfAltiMi = merchant.IsPaymentFacilitatorSub;
            }

            // Kartın gezindiği işyeri çeşitliliği: işyeri kaydı olmasa da kart geçmişinden çıkar.
            // Mevcut işlem de sayılır, tıpkı diğer çeşitlilik sayaçlarında olduğu gibi.
            input.FarkliIsyeriSayisi = cardWindow24H
                .Select(t => t.MerchantId)
                .Append(input.MerchantId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();

            if (merchantHistory is null || string.IsNullOrWhiteSpace(input.MerchantId))
                return;

            // Kart deneme saldırısı işaretidir: aynı POS'ta kısa sürede çok sayıda farklı kart.
            // Kredi ve banka kartı kimlikleri ayrı sayaç uzaylarında olduğu için ön ek ile ayrılır.
            var distinctCards = merchantHistory
                .Where(t => (now - t.TransactionDate) <= TimeSpan.FromHours(1))
                .Select(CardKey)
                .Where(key => key is not null)
                .ToHashSet(StringComparer.OrdinalIgnoreCase)!;

            // Değerlendirilen kart da sayılır. HashSet olduğu için geçmişte zaten
            // görülmüşse tekrar eklenmez — aynı kart iki kez sayılmaz.
            if (cardId > 0)
                distinctCards.Add(isCreditCard ? $"C{cardId}" : $"D{cardId}");

            input.FarkliKartSayisi = distinctCards.Count;

            ApplyMerchantVolumeCounters(input, merchantHistory, now, cardId, isCreditCard);
        }

        /// <summary>
        /// İşyeri geçmişinden hesaplanan hacim, ret ve ilk-kullanım sayaçları.
        /// <para>
        /// Kapsam kart değil <b>işyeridir</b>: aynı POS'ta tüm kartların ürettiği hareket.
        /// Pencere, orkestratörün getirdiği işyeri geçmişi kadardır (24 saat).
        /// </para>
        /// </summary>
        private static void ApplyMerchantVolumeCounters(
            ProcessTransactionInput input,
            IReadOnlyList<ITransaction> merchantHistory,
            DateTime now,
            int cardId,
            bool isCreditCard)
        {
            // S4: işyerinin 24 saatlik toplam cirosu (bu işlem dahil).
            input.IsyeriSonGunHacmi = merchantHistory
                .Where(t => IsApproved(t))
                .Sum(t => t.Amount) + input.Amount;

            // S7: işyerinde son 1 saatte alınan ret adedi.
            // Not: kaynak senaryo Kayıp/Çalıntı yanıt kodlarını (41/43) ayırıyor; yanıt kodu
            // işlem tablolarında saklanmadığı için burada tüm retler sayılır.
            input.IsyeriSonSaatRetAdedi = merchantHistory.Count(t =>
                !IsApproved(t) && (now - t.TransactionDate) <= TimeSpan.FromHours(1));

            // S51: işyerinde son 6 saatte 2.500 TL üzeri işlem adedi (bu işlem dahil).
            input.IsyeriSonAltiSaatYuksekTutarAdedi = merchantHistory.Count(t =>
                t.Amount >= HighValueTransactionFloor &&
                (now - t.TransactionDate) <= TimeSpan.FromHours(6))
                + (input.Amount >= HighValueTransactionFloor ? 1 : 0);

            // S3: işyerinin gece penceresindeki cirosu.
            input.IsyeriGeceIslemHacmi = merchantHistory
                .Where(t => IsApproved(t) && IsNight(t.TransactionDate))
                .Sum(t => t.Amount);

            // S8: işyerinin son işleminden bu yana geçen gün. Uzun sessizlik sonrası
            // gelen ilk işlem ayrı bir tipolojidir.
            var lastMerchantTx = merchantHistory
                .OrderByDescending(t => t.TransactionDate)
                .FirstOrDefault();

            input.IsyeriSonIslemdenGecenGun = lastMerchantTx is null
                ? int.MaxValue
                : (int)Math.Max(0, (now - lastMerchantTx.TransactionDate).TotalDays);

            // S25: işyeri daha önce hiç yüksek tutarlı işlem görmemişse, bugünkü ilk yüksek
            // tutar dikkat çekicidir. Pencere, orkestratörün getirdiği işyeri geçmişi kadardır.
            input.IsyeriSon30GunYuksekTutarVarMi = merchantHistory.Any(t =>
                IsApproved(t) && t.Amount >= MerchantHighValueFloor);

            // S58: bu kart bu işyerinde daha önce hiç kullanılmamışsa ilk kullanımdır.
            string? currentCardKey = cardId > 0
                ? (isCreditCard ? $"C{cardId}" : $"D{cardId}")
                : null;

            input.KartIsyerindeIlkKullanimMi = currentCardKey is not null &&
                !merchantHistory.Any(t => string.Equals(CardKey(t), currentCardKey, StringComparison.OrdinalIgnoreCase));
        }

        private static string? CardKey(ITransaction transaction)
        {
            if (transaction.CreditCardId is int creditId) return $"C{creditId}";
            if (transaction.DebitCardId is int debitId) return $"D{debitId}";
            return null;
        }

        private static void ApplyTimeFields(ProcessTransactionInput input, DateTime now)
        {
            input.IslemZamani = now;
            input.IslemSaati = now.Hour;
            input.HaftaSonuMu = now.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
            input.GeceIslemiMi = IsNight(now);

            // BIN, kart numarasının ilk 6 hanesidir. Ayrı bir alan taşınmadığı için buradan türetilir.
            input.BinNo = input.CardNumber is { Length: >= 6 }
                ? input.CardNumber.Substring(0, 6)
                : string.Empty;
        }

        private static void ApplyVolumeCounters(
            ProcessTransactionInput input, IReadOnlyList<ITransaction> window, DateTime now)
        {
            input.SonGunIslemSayisi = window.Count;
            input.SonGunIslemHacmi = window.Sum(t => t.Amount);

            input.ToplamSatisTutar = window
                .Where(t => t.TransactionTypeId == (int)TransactionTypeEnum.Sale && IsApproved(t))
                .Sum(t => t.Amount);

            input.EnYuksekIslemTutari = window.Count == 0 ? 0m : window.Max(t => t.Amount);
            input.OrtalamaIslemTutari = window.Count == 0 ? 0m : window.Average(t => t.Amount);

            input.GeceIslemAdedi = window.Count(t => IsNight(t.TransactionDate));

            input.BasarisizIslemSayisi = window.Count(t => !IsApproved(t));

            input.BinAltindaOnayliIslemAdedi = window.Count(t =>
                IsApproved(t) &&
                t.Amount < SmallTransactionCeiling &&
                (now - t.TransactionDate) <= TimeSpan.FromHours(1));
        }

        private static void ApplyVelocityCounters(
            ProcessTransactionInput input, IReadOnlyList<ITransaction> window, DateTime now)
        {
            // Mevcut işlem de sayılır: geçmişte 2 + bu işlem = 3
            input.AyniKartIslemAdedi = 1 + window.Count(t =>
                (now - t.TransactionDate) <= TimeSpan.FromHours(1));

            input.IkiDakikadaYapilanIslemAdedi = 1 + window.Count(t =>
                (now - t.TransactionDate) <= TimeSpan.FromMinutes(2));

            input.AyniKartAyniTutarAdedi = 1 + window.Count(t => t.Amount == input.Amount);

            var lastTransaction = window
                .OrderByDescending(t => t.TransactionDate)
                .FirstOrDefault();

            input.SonIslemdenGecenDakika = lastTransaction is null
                ? int.MaxValue
                : (int)Math.Max(0, (now - lastTransaction.TransactionDate).TotalMinutes);
        }

        private static void ApplyRefundCounters(
            ProcessTransactionInput input, IReadOnlyList<ITransaction> window, DateTime now)
        {
            var refunds = window
                .Where(t => t.TransactionTypeId == (int)TransactionTypeEnum.Refund)
                .ToList();

            input.ToplamIadeTutari = refunds.Sum(t => t.Amount);

            input.IkiSaatlikIadeIslemSayisi = refunds.Count(t =>
                (now - t.TransactionDate) <= TimeSpan.FromHours(2));
        }

        private static void ApplyDiversityCounters(
            ProcessTransactionInput input, IReadOnlyList<ITransaction> window)
        {
            input.FarkliKategoriSayisi = window
                .Select(t => t.MerchantCategory)
                .Append(input.MerchantCategory)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();

            input.FarkliUlkeSayisi = window
                .Select(t => t.Country)
                .Append(input.Country)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();
        }

        private static void ApplyLimitAndBalance(
            ProcessTransactionInput input, decimal cardLimit, decimal cardBalance)
        {
            input.KartLimiti = cardLimit;
            input.KalanLimit = cardBalance;

            if (cardLimit > 0)
            {
                input.KalanLimitOrani = Math.Max(0m, (cardBalance - input.Amount) / cardLimit);
                input.LimitKullanimOrani = input.Amount / cardLimit;
            }
            else
            {
                input.KalanLimitOrani = 1.0m;
            }

            if (cardBalance > 0)
            {
                input.BakiyeCekimOrani = input.Amount / cardBalance;
            }
        }

        private static void ApplySecurityAndPatternIndicators(
            ProcessTransactionInput input, IReadOnlyList<ITransaction> window, DateTime now)
        {
            // Yabancı Ülke Kontrolleri
            input.YabanciUlkeMi = !string.IsNullOrWhiteSpace(input.Country) &&
                                  !input.Country.Equals("Türkiye", StringComparison.OrdinalIgnoreCase) &&
                                  !input.Country.Equals("Turkey", StringComparison.OrdinalIgnoreCase) &&
                                  !input.Country.Equals("TR", StringComparison.OrdinalIgnoreCase);

            input.GecmisteKullanilmayanUlkeMi = input.YabanciUlkeMi &&
                                               window.Count > 0 &&
                                               !window.Any(t => string.Equals(t.Country, input.Country, StringComparison.OrdinalIgnoreCase));

            // Para Birimi Sapması
            input.GecmisteKullanilmayanParaBirimiMi = window.Count > 0 &&
                                                     !string.IsNullOrWhiteSpace(input.Currency) &&
                                                     !window.Any(t => string.Equals(t.Currency, input.Currency, StringComparison.OrdinalIgnoreCase));

            // Riskli MCC / Kategori
            var riskliMccList = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Kuyumcu", "Kripto", "Bahis", "Kumar", "Döviz", "Jewelry", "Casino", "Crypto", "Betting"
            };
            input.RiskliMccMi = !string.IsNullOrWhiteSpace(input.MerchantCategory) && riskliMccList.Contains(input.MerchantCategory);

            // Yasaklı Açıklama
            if (!string.IsNullOrWhiteSpace(input.Description))
            {
                var yasakliKelimeler = new[] { "bahis", "kripto", "casino", "kumar", "bet", "forex", "btc", "usdt", "slot" };
                input.YasakliKelimeIceriyorMu = yasakliKelimeler.Any(w => input.Description.IndexOf(w, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            // İmkansız Seyahat: Son 30 dakikadaki son işlem farklı bir şehir veya ülkede mi?
            var recentTx = window
                .Where(t => (now - t.TransactionDate) <= TimeSpan.FromMinutes(45))
                .OrderByDescending(t => t.TransactionDate)
                .FirstOrDefault();

            if (recentTx != null && !string.IsNullOrWhiteSpace(recentTx.Location) && !string.IsNullOrWhiteSpace(input.Location))
            {
                bool differentCity = !string.Equals(recentTx.Location, input.Location, StringComparison.OrdinalIgnoreCase);
                bool differentCountry = !string.Equals(recentTx.Country, input.Country, StringComparison.OrdinalIgnoreCase);
                input.ImkansizSeyahatVarMi = differentCity || differentCountry;
            }

            // ATM Nakit Yatırma Sayaçları
            var atmDeposits = window
                .Where(t => t.TransactionTypeId == (int)TransactionTypeEnum.Deposit)
                .ToList();

            input.SonGunAtmNakitYatirmaSayisi = atmDeposits.Count;

            // Kısa sürede üst üste fonlama, katır hesap yükleme örüntüsünün işaretidir.
            // Kaynak kartı ayırt edemiyoruz (yatırma işlemi kaynak kart bilgisi taşımaz),
            // bu yüzden ölçülen şey "farklı kart" değil "fonlama sıklığı"dır.
            // Değerlendirilen işlem de yatırma ise sayılır — diğer hız sayaçlarıyla aynı kural:
            // "1 saatte 3 yatırma" koşulu, geçmişteki 2 + mevcut işlem ile sağlanır.
            input.SonSaatFonlamaSayisi =
                atmDeposits.Count(t => (now - t.TransactionDate) <= TimeSpan.FromHours(1)) +
                (input.TransactionType == TransactionTypeEnum.Deposit ? 1 : 0);
            input.SonGunAtmNakitYatirmaHacmi = atmDeposits.Sum(t => t.Amount);
            input.GeceNakitYatirmaHacmi = atmDeposits.Where(t => IsNight(t.TransactionDate)).Sum(t => t.Amount);

            // Yatır ve Kaç (Deposit and Run): Son 1 saatte ATM'den para yatmış ve bu işlem yatırılanın %90'ından fazlasını harcıyor mu?
            var recentDeposit = atmDeposits
                .Where(t => (now - t.TransactionDate) <= TimeSpan.FromHours(1))
                .OrderByDescending(t => t.TransactionDate)
                .FirstOrDefault();

            if (recentDeposit != null && recentDeposit.Amount > 0)
            {
                input.NakitYatirmaSonrasiHarcanmaOrani = input.Amount / recentDeposit.Amount;
            }
        }

        /// <summary>
        /// S13: aynı kartın aynı bölgede (lokasyonda) yaptığı işlemler.
        /// <para>
        /// Kaynak senaryo "aynı tutar ya da arttırarak 3. işlem" der; burada bölge olarak
        /// işlemin <see cref="ProcessTransactionInput.Location"/> alanı kullanılır, çünkü
        /// modelde ayrı bir bölge/il kodu yoktur.
        /// </para>
        /// </summary>
        private static void ApplyRegionalCounters(
            ProcessTransactionInput input, IReadOnlyList<ITransaction> window)
        {
            if (string.IsNullOrWhiteSpace(input.Location))
                return;

            var sameRegion = window
                .Where(t => string.Equals(t.Location, input.Location, StringComparison.OrdinalIgnoreCase))
                .OrderBy(t => t.TransactionDate)
                .ToList();

            input.BolgeselAyniKartAdedi = sameRegion.Count + 1;

            // Tutar dizisi azalmıyorsa (sabit ya da artan) örüntü sayılır.
            decimal previous = decimal.MinValue;
            bool nonDecreasing = true;
            foreach (var amount in sameRegion.Select(t => t.Amount).Append(input.Amount))
            {
                if (amount < previous) { nonDecreasing = false; break; }
                previous = amount;
            }

            input.BolgeselTutarArtanVeyaSabitMi = nonDecreasing;
        }

        /// <summary>
        /// BIN tablosu ve operasyonel listelerden türeyen göstergeler.
        /// <para>
        /// BIN bulunamazsa alanlar varsayılanda kalır: kartın yurtdışı olduğunu <b>varsaymak</b>
        /// yanlış pozitif üretir, bilinmiyorsa sessiz kalmak doğrudur.
        /// </para>
        /// </summary>
        private static void ApplyReferenceDataIndicators(
            ProcessTransactionInput input, ReferenceDataContext? reference)
        {
            if (reference is null)
                return;

            if (!string.IsNullOrEmpty(input.MccKodu))
            {
                input.SifresizKapaliMccMi = reference.PinlessBlockedMccs.Contains(input.MccKodu);
                input.KuyumcuMccMi = reference.JewelryMccs.Contains(input.MccKodu);
            }

            if (string.IsNullOrEmpty(input.BinNo) ||
                !reference.BinRanges.TryGetValue(input.BinNo, out var bin))
            {
                return;
            }

            input.KartSemasi = bin.Scheme;
            input.KartUlkesi = bin.CountryCode;
            input.YurtDisiKartMi = !string.Equals(bin.CountryCode, "TR", StringComparison.OrdinalIgnoreCase);

            input.RiskliBinMi = bin.IsRisky;
            input.YasakliBinMi = bin.IsSanctioned;
            input.ExpediaBinMi = bin.IsExpedia;

            input.RiskliUlkeKartiMi = reference.RiskyCountries.Contains(bin.CountryCode);
            input.DurdurulanUlkeMi = reference.BlockedCountries.Contains(bin.CountryCode);
            input.DurdurulanSemaMi = reference.BlockedSchemes.Contains(bin.Scheme);
        }

        private static bool IsApproved(ITransaction transaction) =>
            string.Equals(transaction.Status, TransactionStatuses.Approved, StringComparison.OrdinalIgnoreCase);

        private static bool IsNight(DateTime moment) =>
            moment.Hour >= NightStartHour || moment.Hour < NightEndHour;
    }
}

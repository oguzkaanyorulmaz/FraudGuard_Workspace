using System;
using System.Collections.Generic;
using System.Linq;
using FraudGuard.Domain.Common.Enums;
using FraudGuard.Domain.DomainObjects.TransactionProcessing;
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
    /// Kapsam: geçmiş, karta/IBAN'a ait son 24 saattir. İşyeri bazlı sayaçlar (farklı kart sayısı,
    /// işyeri cirosu vb.) Merchant master verisi sisteme eklenene kadar hesaplanamaz ve
    /// varsayılan değerlerinde kalır.
    /// </para>
    /// </summary>
    public static class TransactionInputEnricher
    {
        private const decimal SmallTransactionCeiling = 1000m;
        private const int NightStartHour = 22;
        private const int NightEndHour = 6;

        public static ProcessTransactionInput Enrich(
            ProcessTransactionInput input,
            IReadOnlyList<ITransaction> history,
            decimal cardLimit = 0m,
            decimal cardBalance = 0m,
            DateTime? evaluatedAt = null)
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
            ApplyLimitAndBalance(input, cardLimit, cardBalance);
            ApplySecurityAndPatternIndicators(input, window24H, now);

            return input;
        }

        private static void ApplyTimeFields(ProcessTransactionInput input, DateTime now)
        {
            input.IslemZamani = now;
            input.IslemSaati = now.Hour;
            input.HaftaSonuMu = now.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
            input.GeceIslemiMi = IsNight(now);
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

        private static bool IsApproved(ITransaction transaction) =>
            string.Equals(transaction.Status, "Approved", StringComparison.OrdinalIgnoreCase);

        private static bool IsNight(DateTime moment) =>
            moment.Hour >= NightStartHour || moment.Hour < NightEndHour;
    }
}

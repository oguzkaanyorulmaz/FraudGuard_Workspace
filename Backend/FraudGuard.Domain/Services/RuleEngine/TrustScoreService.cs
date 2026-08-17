using System.Collections.Generic;
using FraudGuard.Domain.Common.Constants;
using FraudGuard.Domain.DomainObjects.FraudEvaluation;
using FraudGuard.Domain.Interfaces.DomainServices;

namespace FraudGuard.Domain.Services.RuleEngine
{
    /// <summary>
    /// Güven geçmişine göre risk indirimi hesaplar. Yerleşik ve temiz geçmişe sahip
    /// hedeflerde yanlış pozitifleri azaltır.
    /// <para>
    /// Saf hesaplama servisidir: veriyi <see cref="TrustContext"/> ile hazır alır,
    /// repository'ye erişmez. Bu, kuralların birim testini bağımlılıksız kılar.
    /// </para>
    /// </summary>
    public class TrustScoreService : ITrustScoreService
    {
        public TrustAssessment Evaluate(TrustContext context)
        {
            var factors = new List<string>();

            int cardDiscount = CalculateCardDiscount(context, factors);
            int merchantDiscount = CalculateMerchantDiscount(context, factors);

            return new TrustAssessment
            {
                CardDiscount = cardDiscount,
                MerchantDiscount = merchantDiscount,
                AppliedFactors = factors
            };
        }

        private static int CalculateCardDiscount(TrustContext context, List<string> factors)
        {
            int discount = 0;

            if (context.IsCardWhitelisted)
            {
                discount += RiskScoringConstants.TrustDiscountWhitelisted;
                factors.Add($"Kart whitelist'te (-{RiskScoringConstants.TrustDiscountWhitelisted}P)");
            }

            if (context.CardHolderTenureDays >= RiskScoringConstants.EstablishedMerchantMinDays)
            {
                discount += RiskScoringConstants.TrustDiscountEstablishedMerchant;
                factors.Add($"Kart hamili 6 aydan uzun süredir kayıtlı (-{RiskScoringConstants.TrustDiscountEstablishedMerchant}P)");
            }

            // null = geçmiş bilinmiyor; indirim verilmez. Sadece "biliyoruz ve temiz" durumu ödüllendirilir.
            if (context.CardAlarmCountLast90Days == 0)
            {
                discount += RiskScoringConstants.TrustDiscountNoRecentAlarm;
                factors.Add($"Kartta son {RiskScoringConstants.NoAlarmLookbackDays} günde alarm yok (-{RiskScoringConstants.TrustDiscountNoRecentAlarm}P)");
            }

            return discount;
        }

        private static int CalculateMerchantDiscount(TrustContext context, List<string> factors)
        {
            int discount = 0;

            if (context.IsMerchantWhitelisted)
            {
                discount += RiskScoringConstants.TrustDiscountWhitelisted;
                factors.Add($"İşyeri whitelist'te (-{RiskScoringConstants.TrustDiscountWhitelisted}P)");
            }

            if (context.MerchantTenureDays >= RiskScoringConstants.EstablishedMerchantMinDays)
            {
                discount += RiskScoringConstants.TrustDiscountEstablishedMerchant;
                factors.Add($"İşyeri 6 aydan uzun süredir kayıtlı (-{RiskScoringConstants.TrustDiscountEstablishedMerchant}P)");
            }

            if (context.MerchantAlarmCountLast90Days == 0)
            {
                discount += RiskScoringConstants.TrustDiscountNoRecentAlarm;
                factors.Add($"İşyerinde son {RiskScoringConstants.NoAlarmLookbackDays} günde alarm yok (-{RiskScoringConstants.TrustDiscountNoRecentAlarm}P)");
            }

            return discount;
        }
    }
}

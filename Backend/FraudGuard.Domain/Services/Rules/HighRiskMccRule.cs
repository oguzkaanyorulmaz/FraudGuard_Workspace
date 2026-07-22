using FraudGuard.Domain.Interfaces.Entities;
using FraudGuard.Domain.Common.Enums;
using FraudGuard.Domain.DomainObjects.TransactionProcessing;
using FraudGuard.Domain.Entities;
using FraudGuard.Domain.Interfaces.Rules;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FraudGuard.Domain.Services.Rules
{
    public class HighRiskMccRule : IFraudRule
    {
        public string RuleCode => "HIGH_RISK_MCC";
        public string RuleName => "Yüksek Riskli İşyeri Tipi (High-Risk MCC)";

        public Task<(bool IsSuspicious, string? Reason)> EvaluateAsync(ProcessTransactionInput input, List<ITransaction> history)
        {
            if (input.PaymentType == PaymentTypeEnum.CreditCard || input.PaymentType == PaymentTypeEnum.DebitCard)
            {
                string[] highRiskMcc = { "Kuyumcu", "Kripto Para Borsası", "Bahis Sitesi" };
                if (highRiskMcc.Contains(input.MerchantCategory) && input.Amount >= 10000)
                {
                    return Task.FromResult((true, (string?)$"Yüksek riskli işyerinden ({input.MerchantCategory}) 10.000 TL ve üzeri harcama denemesi."));
                }
            }

            return Task.FromResult((false, (string?)null));
        }
    }
}
using FraudGuard.Domain.Common.Enums;
using FraudGuard.Domain.DomainObjects.TransactionProcessing;
using FraudGuard.Domain.Entities;
using FraudGuard.Domain.Interfaces.Rules;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FraudGuard.Domain.Services.Rules
{
    public class CurrencyMismatchRule : IFraudRule
    {
        public string RuleCode => "CURRENCY_MISMATCH";
        public string RuleName => "Para Birimi Sapması (Currency Mismatch)";

        public Task<(bool IsSuspicious, string? Reason)> EvaluateAsync(ProcessTransactionInput input, List<ETransaction> history)
        {
            if (input.PaymentType == PaymentTypeEnum.CreditCard || input.PaymentType == PaymentTypeEnum.DebitCard)
            {
                if (input.Currency != "TRY")
                {
                    bool hasUsedCurrencyBefore = history.Any(t => t.Currency == input.Currency && t.Status == "Approved");
                    if (!hasUsedCurrencyBefore)
                    {
                        return Task.FromResult((true, (string?)$"Geçmişte onaylanmış {input.Currency} işlemi bulunmamasına rağmen döviz cinsinden işlem denemesi."));
                    }
                }
            }

            return Task.FromResult((false, (string?)null));
        }
    }
}

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
    public class CrossBorderRule : IFraudRule
    {
        public string RuleCode => "CROSS_BORDER";
        public string RuleName => "Beklenmedik Sınır Ötesi İşlem (Cross-Border)";

        public Task<(bool IsSuspicious, string? Reason)> EvaluateAsync(ProcessTransactionInput input, List<ITransaction> history)
        {
            if (input.PaymentType == PaymentTypeEnum.CreditCard || input.PaymentType == PaymentTypeEnum.DebitCard)
            {
                bool hasForeignTxBefore = history.Any(t => t.Country != "Türkiye");
                if (!hasForeignTxBefore && input.Country != "Türkiye")
                {
                    return Task.FromResult((true, (string?)"Müşterinin geçmişinde yurt dışı işlemi bulunmamasına rağmen aniden sınır ötesi işlem denendi."));
                }
            }

            return Task.FromResult((false, (string?)null));
        }
    }
}
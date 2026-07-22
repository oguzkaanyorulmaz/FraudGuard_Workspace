using FraudGuard.Domain.Interfaces.Entities;
using FraudGuard.Domain.Common.Enums;
using FraudGuard.Domain.DomainObjects.TransactionProcessing;
using FraudGuard.Domain.Interfaces.Rules;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FraudGuard.Domain.Services.Rules
{
    public class HighValueRefundRule : IFraudRule
    {
        public string RuleCode => "HIGH_VALUE_REFUND_VOID";
        public string RuleName => "Yüksek Tutarlı İade Kuralı (High Value Refund)";

        public Task<(bool IsSuspicious, string? Reason)> EvaluateAsync(ProcessTransactionInput input, List<ITransaction> history)
        {
            if (input.TransactionType == TransactionTypeEnum.Refund)
            {
                // Trigger if the amount is greater than 10,000 TRY
                if (input.Amount > 10000)
                {
                    return Task.FromResult((true, (string?)$"Yüksek tutarlı iade işlemi denendi (Tutar: {input.Amount} {input.Currency}, Limit: 10.000)."));
                }
            }

            return Task.FromResult((false, (string?)null));
        }
    }
}

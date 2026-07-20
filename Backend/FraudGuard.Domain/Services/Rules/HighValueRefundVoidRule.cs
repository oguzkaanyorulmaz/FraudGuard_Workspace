using FraudGuard.Domain.Common.Enums;
using FraudGuard.Domain.DomainObjects.TransactionProcessing;
using FraudGuard.Domain.Entities;
using FraudGuard.Domain.Interfaces.Rules;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FraudGuard.Domain.Services.Rules
{
    public class HighValueRefundVoidRule : IFraudRule
    {
        public string RuleCode => "HIGH_VALUE_REFUND_VOID";
        public string RuleName => "Yüksek Tutarlı İptal/İade Kuralı (High Value Refund/Void)";

        public Task<(bool IsSuspicious, string? Reason)> EvaluateAsync(ProcessTransactionInput input, List<ETransaction> history)
        {
            if (input.TransactionType == TransactionTypeEnum.Refund || input.TransactionType == TransactionTypeEnum.Void)
            {
                // Trigger if the amount is greater than 10,000 TRY
                if (input.Amount > 10000)
                {
                    return Task.FromResult((true, (string?)$"Yüksek tutarlı iade/iptal işlemi denendi (Tutar: {input.Amount} {input.Currency}, Limit: 10.000)."));
                }
            }

            return Task.FromResult((false, (string?)null));
        }
    }
}

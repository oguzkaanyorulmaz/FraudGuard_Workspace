using FraudGuard.Domain.Common.Enums;
using FraudGuard.Domain.DomainObjects.TransactionProcessing;
using FraudGuard.Domain.Entities;
using FraudGuard.Domain.Interfaces.Rules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FraudGuard.Domain.Services.Rules
{
    public class ConsecutiveRefundsRule : IFraudRule
    {
        public string RuleCode => "CONSECUTIVE_REFUNDS";
        public string RuleName => "Ardışık İade Kuralı (Consecutive Refunds)";

        public Task<(bool IsSuspicious, string? Reason)> EvaluateAsync(ProcessTransactionInput input, List<ETransaction> history)
        {
            if (input.PaymentType == PaymentTypeEnum.CreditCard || input.PaymentType == PaymentTypeEnum.DebitCard)
            {
                if (input.TransactionType == TransactionTypeEnum.Refund)
                {
                    // Count refunds in the last 24 hours
                    int refundCount = history
                        .Count(t => t.Status == "Refund" && t.RefundTime.HasValue && t.RefundTime.Value >= DateTime.Now.AddDays(-1));

                    if (refundCount >= 2)
                    {
                        return Task.FromResult((true, (string?)"Kart üzerinde son 24 saat içinde 3 veya daha fazla iade (Refund) işlemi yapılmaya çalışılıyor."));
                    }
                }
            }

            return Task.FromResult((false, (string?)null));
        }
    }
}

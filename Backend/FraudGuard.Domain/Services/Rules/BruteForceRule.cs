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
    public class BruteForceRule : IFraudRule
    {
        public string RuleCode => "BRUTE_FORCE";
        public string RuleName => "Ardışık Hata / Brute Force (Brute Force)";

        public Task<(bool IsSuspicious, string? Reason)> EvaluateAsync(ProcessTransactionInput input, List<ETransaction> history)
        {
            if (input.PaymentType == PaymentTypeEnum.CreditCard || input.PaymentType == PaymentTypeEnum.DebitCard)
            {
                var last30MinDeclines = history
                    .Where(t => t.TransactionDate <= DateTime.Now && (DateTime.Now - t.TransactionDate).TotalMinutes <= 30)
                    .OrderByDescending(t => t.TransactionDate)
                    .ToList();

                int consecutiveDeclines = 0;
                foreach (var tx in last30MinDeclines)
                {
                    if (tx.Status == "Declined") consecutiveDeclines++;
                    else if (tx.Status == "Approved") break;
                }

                if (consecutiveDeclines >= 3)
                {
                    return Task.FromResult((true, (string?)"Son 30 dakikada üst üste 3 işlem reddinden sonra 4. deneme yapılmaktadır."));
                }
            }

            return Task.FromResult((false, (string?)null));
        }
    }
}
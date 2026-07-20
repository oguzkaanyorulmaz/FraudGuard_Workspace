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
    public class VelocityRule : IFraudRule
    {
        public string RuleCode => "VELOCITY";
        public string RuleName => "Hız/Sıklık Kuralı (Velocity)";

        public Task<(bool IsSuspicious, string? Reason)> EvaluateAsync(ProcessTransactionInput input, List<ETransaction> history)
        {
            if (input.TransactionType != TransactionTypeEnum.Sale)
            {
                return Task.FromResult((false, (string?)null));
            }

            if (input.PaymentType == PaymentTypeEnum.CreditCard || input.PaymentType == PaymentTypeEnum.DebitCard)
            {
                var countInLast5Mins = history.Count(t => 
                    t.TransactionDate <= DateTime.Now &&
                    (DateTime.Now - t.TransactionDate).TotalMinutes <= 5 && 
                    t.Status == "Approved" && 
                    t.TransactionTypeId == 1);

                if (countInLast5Mins >= 3)
                {
                    return Task.FromResult((true, (string?)"Aynı kartla son 5 dakika içinde 3 veya daha fazla işlem yapıldı."));
                }
            }

            return Task.FromResult((false, (string?)null));
        }
    }
}
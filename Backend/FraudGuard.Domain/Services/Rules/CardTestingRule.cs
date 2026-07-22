using FraudGuard.Domain.Interfaces.Entities;
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
    public class CardTestingRule : IFraudRule
    {
        public string RuleCode => "CARD_TESTING";
        public string RuleName => "Yoklama/Deneme Çekimi (Card Testing)";

        public Task<(bool IsSuspicious, string? Reason)> EvaluateAsync(ProcessTransactionInput input, List<ITransaction> history)
        {
            if (input.PaymentType == PaymentTypeEnum.CreditCard || input.PaymentType == PaymentTypeEnum.DebitCard)
            {
                var smallTestTx = history.FirstOrDefault(t => 
                    t.TransactionDate <= DateTime.Now &&
                    t.Amount <= 10 && 
                    (DateTime.Now - t.TransactionDate).TotalMinutes <= 10);

                if (smallTestTx != null && input.Amount >= 20000)
                {
                    return Task.FromResult((true, (string?)"10 dakika içinde yapılan mikro deneme (1-10 TL) onayından hemen sonra yüksek tutarlı harcama denemesi."));
                }
            }

            return Task.FromResult((false, (string?)null));
        }
    }
}
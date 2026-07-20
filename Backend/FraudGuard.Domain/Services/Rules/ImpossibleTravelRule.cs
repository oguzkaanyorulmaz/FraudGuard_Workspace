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
    public class ImpossibleTravelRule : IFraudRule
    {
        public string RuleCode => "IMPOSSIBLE_TRAVEL";
        public string RuleName => "Lokasyon/Mesafe Kuralı (Impossible Travel)";

        public Task<(bool IsSuspicious, string? Reason)> EvaluateAsync(ProcessTransactionInput input, List<ETransaction> history)
        {
            if (input.TransactionType != TransactionTypeEnum.Sale)
            {
                return Task.FromResult((false, (string?)null));
            }

            if (input.PaymentType == PaymentTypeEnum.CreditCard || input.PaymentType == PaymentTypeEnum.DebitCard)
            {
                var lastTx = history
                    .OrderByDescending(t => t.TransactionDate)
                    .FirstOrDefault(t => t.Status == "Approved" || t.Status == "Refund" || t.Status == "Void");

                if (lastTx != null && !string.IsNullOrEmpty(lastTx.Location) && !string.IsNullOrEmpty(input.Location) && lastTx.Location != input.Location)
                {
                    var timeDiff = Math.Abs((DateTime.Now - lastTx.TransactionDate).TotalMinutes);
                    if (timeDiff <= 10)
                    {
                        return Task.FromResult((true, (string?)$"10 dakika arayla iki farklı lokasyonda ({lastTx.Location} -> {input.Location}) işlem denendi."));
                    }
                }
            }

            return Task.FromResult((false, (string?)null));
        }
    }
}
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
    public class SmurfingRule : IFraudRule
    {
        public string RuleCode => "SMURFING";
        public string RuleName => "Dilimleme / Parçalayarak Transfer (Smurfing)";

        public Task<(bool IsSuspicious, string? Reason)> EvaluateAsync(ProcessTransactionInput input, List<ITransaction> history)
        {
            if (input.PaymentType == PaymentTypeEnum.EFT || input.PaymentType == PaymentTypeEnum.BankTransfer)
            {
                var hourlyTransfers = history
                    .Where(t => t.TransactionDate <= DateTime.Now && (DateTime.Now - t.TransactionDate).TotalHours <= 1 && (t.PaymentType == PaymentTypeEnum.BankTransfer || t.PaymentType == PaymentTypeEnum.EFT))
                    .ToList();
                decimal totalHourlyAmount = hourlyTransfers.Sum(t => t.Amount) + input.Amount;
                if (hourlyTransfers.Count >= 2 && totalHourlyAmount >= 50000 && input.Amount < 50000)
                {
                    return Task.FromResult((true, (string?)"Yasal bildirim limitini (50.000 TL) aşmamak amacıyla transferlerin küçük parçalara bölünmesi şüphesi."));
                }
            }

            return Task.FromResult((false, (string?)null));
        }
    }
}
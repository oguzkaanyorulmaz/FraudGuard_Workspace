using FraudGuard.Domain.Interfaces.Entities;
using FraudGuard.Domain.Common.Enums;
using FraudGuard.Domain.DomainObjects.TransactionProcessing;
using FraudGuard.Domain.Interfaces.Rules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FraudGuard.Domain.Services.Rules
{
    public class DepositLimitAvoidanceRule : IFraudRule
    {
        public string RuleCode => "DEPOSIT_LIMIT_AVOIDANCE";
        public string RuleName => "Yapılandırılmış Aklama Kuralı (Smurfing / Deposit Limit Avoidance)";

        public Task<(bool IsSuspicious, string? Reason)> EvaluateAsync(ProcessTransactionInput input, List<ITransaction> history)
        {
            // Sadece para yatırma işlemlerinde tetiklenir
            if (input.TransactionType == TransactionTypeEnum.Deposit)
            {
                // Son 24 saatteki başarılı ATM para yatırma işlemlerini (TransactionTypeId = 3) al
                var pastDeposits = history
                    .Where(t => t.TransactionTypeId == 3 
                                && t.Status == "Approved" 
                                && (DateTime.Now - t.TransactionDate).TotalHours <= 24)
                    .ToList();

                // ATM lokasyonlarını çıkar
                var locations = pastDeposits.Select(t => t.Location).Distinct().ToList();
                if (!locations.Contains(input.Location))
                {
                    locations.Add(input.Location);
                }

                decimal totalDeposited = pastDeposits.Sum(t => t.Amount) + input.Amount;

                // 24 saat içinde 3 veya daha fazla farklı ATM'den toplamda 40.000 TL ve üzeri para yatırma denemesi
                if (locations.Count >= 3 && totalDeposited >= 40000)
                {
                    return Task.FromResult((true, (string?)$"Son 24 saat içinde {locations.Count} farklı ATM'den toplamda {totalDeposited:N2} {input.Currency} tutarında parçalı para yatırma işlemi gerçekleştirilerek limit kaçınma şüphesi."));
                }
            }

            return Task.FromResult((false, (string?)null));
        }
    }
}

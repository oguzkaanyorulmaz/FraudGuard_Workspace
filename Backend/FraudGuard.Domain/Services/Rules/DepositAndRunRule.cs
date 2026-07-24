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
    public class DepositAndRunRule : IFraudRule
    {
        public string RuleCode => "DEPOSIT_AND_RUN";
        public string RuleName => "Yatır ve Kaç Kuralı (Deposit and Run)";

        public Task<(bool IsSuspicious, string? Reason)> EvaluateAsync(ProcessTransactionInput input, List<ITransaction> history)
        {
            // Bu kural, para çıkışı olan bir harcama (Sale) işlemi yapıldığında tetiklenir
            if (input.TransactionType == TransactionTypeEnum.Sale)
            {
                // Son 10 dakika içindeki başarılı ATM para yatırma işlemlerini (TransactionTypeId = 3) filtrele
                var recentDeposits = history
                    .Where(t => t.TransactionTypeId == 3 
                                && t.Status == "Approved" 
                                && (DateTime.Now - t.TransactionDate).TotalMinutes <= 10)
                    .ToList();

                if (recentDeposits.Any())
                {
                    decimal totalDeposited = recentDeposits.Sum(t => t.Amount);
                    
                    // Eğer harcanmak istenen tutar, son 10 dakikada yatırılan toplam tutarın %90'ından fazla ise
                    if (input.Amount >= totalDeposited * 0.90m)
                    {
                        return Task.FromResult((true, (string?)$"Son 10 dakika içinde hesaba ATM'den {totalDeposited:N2} {input.Currency} yatırıldıktan hemen sonra bu tutarın %90'ından fazlası ({input.Amount:N2} {input.Currency}) harcanmak isteniyor."));
                    }
                }
            }

            return Task.FromResult((false, (string?)null));
        }
    }
}

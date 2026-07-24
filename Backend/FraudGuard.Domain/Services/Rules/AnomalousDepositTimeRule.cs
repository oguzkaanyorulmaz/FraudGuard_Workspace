using FraudGuard.Domain.Interfaces.Entities;
using FraudGuard.Domain.Common.Enums;
using FraudGuard.Domain.DomainObjects.TransactionProcessing;
using FraudGuard.Domain.Interfaces.Rules;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FraudGuard.Domain.Services.Rules
{
    public class AnomalousDepositTimeRule : IFraudRule
    {
        public string RuleCode => "ANOMALOUS_DEPOSIT_TIME";
        public string RuleName => "Gece Yarısı Nakit Akışı Kuralı (Anomalous Deposit Time)";

        public Task<(bool IsSuspicious, string? Reason)> EvaluateAsync(ProcessTransactionInput input, List<ITransaction> history)
        {
            // Sadece para yatırma işlemlerinde tetiklenir
            if (input.TransactionType == TransactionTypeEnum.Deposit)
            {
                var hour = DateTime.Now.Hour;
                // Gece 23:00 ile sabah 06:00 saatleri arası
                bool isSuspiciousHour = hour >= 23 || hour < 6;

                // Gece vakti 10.000 TL ve üzeri para yatırma
                if (isSuspiciousHour && input.Amount >= 10000m)
                {
                    return Task.FromResult((true, (string?)$"Gece geç saatte ({hour:D2}:00) ATM'den {input.Amount:N2} {input.Currency} tutarında olağan dışı yüksek nakit para yatırma işlemi."));
                }
            }

            return Task.FromResult((false, (string?)null));
        }
    }
}

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
    public class LateVoidRule : IFraudRule
    {
        public string RuleCode => "LATE_VOID";
        public string RuleName => "Gecikmiş İptal Kuralı (Late Void)";

        public Task<(bool IsSuspicious, string? Reason)> EvaluateAsync(ProcessTransactionInput input, List<ETransaction> history)
        {
            if (input.TransactionType == TransactionTypeEnum.Void && input.OriginalTransactionId.HasValue)
            {
                var originalTx = history.FirstOrDefault(t => t.TransactionId == input.OriginalTransactionId.Value);
                if (originalTx != null)
                {
                    // If more than 2 hours have passed since the original Sale transaction date
                    if (DateTime.Now - originalTx.TransactionDate > TimeSpan.FromHours(2))
                    {
                        return Task.FromResult((true, (string?)$"Orijinal işlem tarihinden ({originalTx.TransactionDate}) 2 saatten fazla süre geçtikten sonra iptal (Void) işlemi yapılmaya çalışılıyor."));
                    }
                }
            }

            return Task.FromResult((false, (string?)null));
        }
    }
}

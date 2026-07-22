using FraudGuard.Domain.Interfaces.Entities;
using FraudGuard.Domain.DomainObjects.TransactionProcessing;
using FraudGuard.Domain.Entities;
using FraudGuard.Domain.Interfaces.Rules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FraudGuard.Domain.Services.Rules
{
    public class SuspiciousDescriptionRule : IFraudRule
    {
        public string RuleCode => "SUSPICIOUS_DESCRIPTION";
        public string RuleName => "Şüpheli İşlem Açıklaması (EFT/Havale)";

        public Task<(bool IsSuspicious, string? Reason)> EvaluateAsync(ProcessTransactionInput input, List<ITransaction> history)
        {
            if (!string.IsNullOrEmpty(input.Description))
            {
                string[] blacklistedWords = { "bahis", "kripto", "kumar", "yasadışı", "giftcard", "borç kapatma" };
                if (blacklistedWords.Any(word => input.Description.ToLower().Contains(word)))
                {
                    return Task.FromResult((true, (string?)$"İşlem açıklamasında yasaklı kelime tespit edildi: '{input.Description}'"));
                }
            }

            return Task.FromResult((false, (string?)null));
        }
    }
}
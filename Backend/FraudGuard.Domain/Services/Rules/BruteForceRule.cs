using FraudGuard.Domain.Entities;
using FraudGuard.Domain.Interfaces.Rules;
using System.Collections.Generic;
using System.Linq;

namespace FraudGuard.Domain.Services.Rules
{
    public class BruteForceRule : IFraudRule
    {
        public int RuleId => 5; 
        public string RuleName => "Ardışık Hata / Deneme (Brute Force)";

        public bool IsSuspicious(ETransaction currentTx, List<ETransaction> history)
        {
            int maxAllowedFailures = 3;

            var lastFailedTransactions = history
                .Where(tx => tx.Status == "Declined" && tx.DeclineReason == "Hatalı CVV")
                .OrderByDescending(tx => tx.TransactionDate)
                .Take(maxAllowedFailures)
                .ToList();

            if (lastFailedTransactions.Count < maxAllowedFailures)
            {
                return false;
            }

            return true;
        }
    }
}
using FraudGuard.Domain.Entities;
using FraudGuard.Domain.Interfaces.Rules;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FraudGuard.Domain.Services.Rules
{
    public class ImpossibleTravelRule : IFraudRule
    {
        public int RuleId => 2; 
        public string RuleName => "Lokasyon/Mesafe Kuralı (Impossible Travel)";

        public bool IsSuspicious(ETransaction currentTx, List<ETransaction> history)
        {
            int impossibleTimeWindowMinutes = 10;

            DateTime timeLimit = currentTx.TransactionDate.AddMinutes(-impossibleTimeWindowMinutes);

            var lastApprovedTx = history
                .Where(tx => tx.Status == "Approved" && tx.TransactionDate >= timeLimit)
                .OrderByDescending(tx => tx.TransactionDate)
                .FirstOrDefault();

            if (lastApprovedTx != null)
            {
                if (lastApprovedTx.Location.Trim().ToLower() != currentTx.Location.Trim().ToLower())
                {
                    return true;
                }
            }

            return false;
        }
    }
}
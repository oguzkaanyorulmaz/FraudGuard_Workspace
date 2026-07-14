using FraudGuard.Domain.Entities;
using FraudGuard.Domain.Interfaces.Rules;
using FraudGuard.Domain.Common.Constants; // <-- Sabitleri dahil ettik
using System;
using System.Collections.Generic;
using System.Linq;

namespace FraudGuard.Domain.Services.Rules
{
    public class VelocityRule : IFraudRule
    {
        public int RuleId => 1; 
        public string RuleName => "Hız/Sıklık Kuralı (Velocity)";

        public bool IsSuspicious(ETransaction currentTx, List<ETransaction> history)
        {
            DateTime timeLimit = currentTx.TransactionDate.AddMinutes(-RuleThresholdConstants.VelocityTimeWindowMinutes);

            int recentTransactionCount = history.Count(tx => 
                tx.TransactionDate >= timeLimit && 
                tx.TransactionDate <= currentTx.TransactionDate &&
                tx.Status == "Approved");

            if (recentTransactionCount >= RuleThresholdConstants.VelocityMaxAllowed)
            {
                return true; 
            }

            return false;
        }
    }
}
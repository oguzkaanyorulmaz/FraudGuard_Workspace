using FraudGuard.Domain.Entities;
using FraudGuard.Domain.Interfaces.Rules;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FraudGuard.Domain.Services.Rules
{
    public class CardTestingRule : IFraudRule
    {
        public int RuleId => 4; 
        public string RuleName => "Yoklama Çekimi (Card Testing)";

        public bool IsSuspicious(ETransaction currentTx, List<ETransaction> history)
        {
            decimal microAmountThreshold = 5m;
            decimal macroAmountThreshold = 20000m;
            int timeWindowMinutes = 10;

            if (currentTx.Amount >= macroAmountThreshold)
            {
                DateTime timeLimit = currentTx.TransactionDate.AddMinutes(-timeWindowMinutes);

                bool hasRecentMicroTx = history.Any(tx => 
                    tx.Status == "Approved" && 
                    tx.Amount <= microAmountThreshold && 
                    tx.TransactionDate >= timeLimit);

                if (hasRecentMicroTx)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
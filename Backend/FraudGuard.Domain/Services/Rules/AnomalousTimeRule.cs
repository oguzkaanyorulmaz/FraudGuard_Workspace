using FraudGuard.Domain.Entities;
using FraudGuard.Domain.Interfaces.Rules;
using System.Collections.Generic;

namespace FraudGuard.Domain.Services.Rules
{
    public class AnomalousTimeRule : IFraudRule
    {
        public int RuleId => 3; 
        public string RuleName => "Zaman ve Tutar Kuralı (Anomalous Time)";

        public bool IsSuspicious(ETransaction currentTx, List<ETransaction> history)
        {
            int startNightHour = 2; 
            int endNightHour = 5;   
            decimal highAmountThreshold = 100000m;

            int currentHour = currentTx.TransactionDate.Hour;

            bool isNightTime = currentHour >= startNightHour && currentHour <= endNightHour;
            
            bool isHighAmount = currentTx.Amount >= highAmountThreshold;

            if (isNightTime && isHighAmount)
            {
                return true; 
            }

            return false;
        }
    }
}
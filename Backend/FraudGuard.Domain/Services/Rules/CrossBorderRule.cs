using FraudGuard.Domain.Entities;
using FraudGuard.Domain.Interfaces.Rules;
using System.Collections.Generic;
using System.Linq;

namespace FraudGuard.Domain.Services.Rules
{
    public class CrossBorderRule : IFraudRule
    {
        public int RuleId => 6; 
        public string RuleName => "Sınır Ötesi İşlem (Cross Border)";

        public bool IsSuspicious(ETransaction currentTx, List<ETransaction> history)
        {
            if (currentTx.Country.Trim().ToLower() != "türkiye")
            {
                bool hasPreviousForeignTx = history.Any(tx => 
                    tx.Country.Trim().ToLower() == currentTx.Country.Trim().ToLower() && 
                    tx.Status == "Approved");

                if (!hasPreviousForeignTx)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
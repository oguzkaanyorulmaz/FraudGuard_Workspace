using FraudGuard.Domain.Entities;
using FraudGuard.Domain.Interfaces.Rules;
using System.Collections.Generic;
using System.Linq;

namespace FraudGuard.Domain.Services.Rules
{
    public class HighRiskMccRule : IFraudRule
    {
        public int RuleId => 7; 
        public string RuleName => "Yüksek Riskli İşyeri (High-Risk MCC)";

        public bool IsSuspicious(ETransaction currentTx, List<ETransaction> history)
        {
            var highRiskCategories = new List<string> { "Kuyumcu", "Kripto Para Borsası", "Bahis Sitesi" };
            
            bool isHighRiskCategory = highRiskCategories.Contains(currentTx.MerchantCategory);
            bool isHighAmount = currentTx.Amount >= 20000m;

            if (isHighRiskCategory && isHighAmount)
            {
                bool hasPreviousHighRiskTx = history.Any(tx => 
                    highRiskCategories.Contains(tx.MerchantCategory) && 
                    tx.Status == "Approved");

                if (!hasPreviousHighRiskTx)
                {
                    return true; 
                }
            }

            return false;
        }
    }
}
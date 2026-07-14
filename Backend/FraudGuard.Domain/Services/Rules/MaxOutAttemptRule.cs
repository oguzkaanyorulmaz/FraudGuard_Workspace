using FraudGuard.Domain.Entities;
using FraudGuard.Domain.Interfaces.Rules;
using System.Collections.Generic;

namespace FraudGuard.Domain.Services.Rules
{
    public class MaxOutAttemptRule : IFraudRule
    {
        public int RuleId => 8; 
        public string RuleName => "Limit Boşaltma Denemesi (Max-Out Attempt)";

        public bool IsSuspicious(ETransaction currentTx, List<ETransaction> history)
        {
            if (currentTx.CreditCard != null && currentTx.CreditCard.CardLimit > 0)
            {
                decimal maxOutThreshold = currentTx.CreditCard.CardLimit * 0.95m;

                if (currentTx.Amount >= maxOutThreshold)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
using FraudGuard.Domain.Entities;
using System.Collections.Generic;

namespace FraudGuard.Domain.Interfaces.Rules
{
    public interface IFraudRule
    {
        int RuleId { get; } 
        
        string RuleName { get; }

        bool IsSuspicious(ETransaction currentTx, List<ETransaction> history);
    }
}
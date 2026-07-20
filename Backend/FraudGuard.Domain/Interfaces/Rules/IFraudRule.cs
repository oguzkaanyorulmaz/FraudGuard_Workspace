using FraudGuard.Domain.DomainObjects.TransactionProcessing;
using FraudGuard.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FraudGuard.Domain.Interfaces.Rules
{
    public interface IFraudRule
    {
        string RuleCode { get; }
        string RuleName { get; }
        Task<(bool IsSuspicious, string? Reason)> EvaluateAsync(ProcessTransactionInput input, List<ETransaction> history);
    }
}
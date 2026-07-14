using FraudGuard.Domain.DomainObjects.TransactionProcessing;
using System.Threading.Tasks;

namespace FraudGuard.Domain.Interfaces.DomainServices
{
    public interface IFraudEvaluationService
    {
        Task<(string? RuleCode, string? FraudReason)> EvaluateAsync(ProcessTransactionInput input, int cardId);        
        Task CreateFraudLogAsync(int transactionId, string ruleCode);
    }
}
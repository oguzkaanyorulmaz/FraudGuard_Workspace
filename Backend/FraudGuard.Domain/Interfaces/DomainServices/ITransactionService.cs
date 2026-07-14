using FraudGuard.Domain.DomainObjects.TransactionProcessing;
using System.Threading.Tasks;

namespace FraudGuard.Domain.Interfaces.DomainServices
{
    public interface ITransactionService
    {
        Task<TransactionCheckResult> ProcessTransactionAsync(ProcessTransactionInput input);
    }
}
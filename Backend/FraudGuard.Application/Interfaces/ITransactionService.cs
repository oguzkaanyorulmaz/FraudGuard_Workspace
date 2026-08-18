using FraudGuard.Domain.DomainObjects.TransactionProcessing;
using System.Threading.Tasks;

namespace FraudGuard.Application.Interfaces
{
    public interface ITransactionService
    {
        Task<TransactionCheckResult> ProcessTransactionAsync(ProcessTransactionInput input);
    }
}
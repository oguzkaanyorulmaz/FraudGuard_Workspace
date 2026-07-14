using System.Threading.Tasks;
using FraudGuard.Application.DTOs.TransactionProcessing;
using FraudGuard.Application.DTOs;

namespace FraudGuard.Application.Interfaces
{
    public interface ITransactionAppService
    {
        Task<ResponseDTO<ProcessTransactionResponse>> ProcessAsync(ProcessTransactionRequest request);
    }
}
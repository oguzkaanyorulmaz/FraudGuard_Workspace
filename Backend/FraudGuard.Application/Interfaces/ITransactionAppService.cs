using FraudGuard.Application.DTOs;
using FraudGuard.Application.DTOs.TransactionProcessing;
using System.Threading.Tasks;

namespace FraudGuard.Application.Interfaces
{
    public interface ITransactionAppService
    {
        Task<ResponseDTO<ProcessTransactionResponse>> ProcessAsync(ProcessTransactionRequest request);
        Task<ResponseDTO<ProcessTransactionResponse>> ProcessTransferAsync(ProcessTransferRequest request); // [YENİ]
    }
}

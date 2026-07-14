using AutoMapper;
using FraudGuard.Application.DTOs;
using FraudGuard.Application.DTOs.TransactionProcessing;
using FraudGuard.Application.Interfaces;
using FraudGuard.Application.Validations;
using FraudGuard.Domain.Interfaces.DomainServices;
using FraudGuard.Domain.DomainObjects.TransactionProcessing;
using System.Threading.Tasks;

namespace FraudGuard.Application.Services
{
    public class TransactionAppService : ITransactionAppService
    {
        private readonly ITransactionService _transactionService;
        private readonly IMapper _mapper;

        public TransactionAppService(ITransactionService transactionService, IMapper mapper)
        {
            _transactionService = transactionService;
            _mapper = mapper;
        }

        public async Task<ResponseDTO<ProcessTransactionResponse>> ProcessAsync(ProcessTransactionRequest request)
        {
            var validator = new ProcessTransactionValidator();
            var validationResult = validator.Validate(request);
            if (!validationResult.IsValid)
                return ResponseDTO<ProcessTransactionResponse>.Fail("Validasyon hatası.");

            var input = _mapper.Map<ProcessTransactionInput>(request);

            var result = await _transactionService.ProcessTransactionAsync(input);


            var response = _mapper.Map<ProcessTransactionResponse>(result);

            string resultMessage = response.Status switch
            {
                "Declined" => "İşlem reddedildi.",
                "Suspicious" => "İşlem şüpheli bulunarak incelemeye alındı.",
                _ => "İşlem onaylandı."
            };

            return ResponseDTO<ProcessTransactionResponse>.Success(response, resultMessage);
        }
    }
}
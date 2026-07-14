using AutoMapper;
using FraudGuard.Application.DTOs.TransactionProcessing;
using FraudGuard.Domain.DomainObjects.TransactionProcessing;

namespace FraudGuard.Application.Mappings
{
    public class TransactionMappingProfile : Profile
    {
        public TransactionMappingProfile()
        {
            CreateMap<ProcessTransactionRequest, ProcessTransactionInput>();
            
            CreateMap<TransactionCheckResult, ProcessTransactionResponse>();
        }
    }
}
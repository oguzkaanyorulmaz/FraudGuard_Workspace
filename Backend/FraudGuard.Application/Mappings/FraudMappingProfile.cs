using AutoMapper;
using FraudGuard.Application.DTOs.FraudManagement;
using FraudGuard.Application.DTOs.RuleManagement;
using FraudGuard.Domain.Entities;
using FraudGuard.Application.Extensions;

namespace FraudGuard.Application.Mappings
{
    public class FraudMappingProfile : Profile
    {
        public FraudMappingProfile()
        {
            CreateMap<EFraudLog, GetUnresolvedLogsResponse>()
                .ForMember(dest => dest.RuleName, opt => opt.MapFrom(src => src.FraudRule.RuleName))
                .ForMember(dest => dest.RuleCode, opt => opt.MapFrom(src => src.FraudRule.RuleCode))
                .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.Transaction.Amount))
                .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.Transaction.Currency))
                .ForMember(dest => dest.MaskedCardNumber, opt => opt.MapFrom(src => src.Transaction.CreditCard.CardNumber.MaskCardNumber()))
                .ForMember(dest => dest.TransactionDate, opt => opt.MapFrom(src => src.Transaction.TransactionDate));

            CreateMap<EFraudRule, GetActiveRulesResponse>();
        }
    }
}
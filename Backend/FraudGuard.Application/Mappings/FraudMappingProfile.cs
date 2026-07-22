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
                .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.Transaction != null ? src.Transaction.Amount : 0))
                .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.Transaction != null ? src.Transaction.Currency : "TRY"))
                .ForMember(dest => dest.MaskedCardNumber, opt => opt.MapFrom(src => 
                    src.CreditCardTransaction != null && src.CreditCardTransaction.CreditCard != null ? src.CreditCardTransaction.CreditCard.CardNumber.MaskCardNumber() :
                    (src.DebitCardTransaction != null && src.DebitCardTransaction.DebitCard != null ? src.DebitCardTransaction.DebitCard.CardNumber.MaskCardNumber() :
                     (src.TransferTransaction != null ? src.TransferTransaction.SenderIBAN : "Bilinmiyor"))))
                .ForMember(dest => dest.TransactionDate, opt => opt.MapFrom(src => src.Transaction != null ? src.Transaction.TransactionDate : System.DateTime.Now))
                .ForMember(dest => dest.TransactionId, opt => opt.MapFrom(src => src.Transaction != null ? src.Transaction.TransactionId : 0))
                .ForMember(dest => dest.PaymentTypeCode, opt => opt.MapFrom(src => 
                    src.Transaction == null ? "Unknown" : src.Transaction.PaymentType.ToString()));


            CreateMap<EFraudRule, GetActiveRulesResponse>();
        }
    }
}
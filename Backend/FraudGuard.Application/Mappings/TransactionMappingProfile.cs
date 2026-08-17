using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using FraudGuard.Application.DTOs.TransactionProcessing;
using FraudGuard.Application.Helpers;
using FraudGuard.Domain.Common.Enums;
using FraudGuard.Domain.DomainObjects.FraudEvaluation;
using FraudGuard.Domain.DomainObjects.TransactionProcessing;

namespace FraudGuard.Application.Mappings
{
    public class TransactionMappingProfile : Profile
    {
        public TransactionMappingProfile()
        {
            CreateMap<ProcessTransactionRequest, ProcessTransactionInput>();
            CreateMap<ProcessTransferRequest, ProcessTransactionInput>();

            CreateMap<TriggeredRule, TriggeredRuleDto>()
                .ForMember(d => d.Target, o => o.MapFrom(s => s.Target.ToString()))
                .ForMember(d => d.Category, o => o.MapFrom(s => s.Category.ToString()));

            CreateMap<AppliedCombination, AppliedCombinationDto>()
                .ForMember(d => d.RuleCodes, o => o.MapFrom(s => string.Join(", ", s.RuleCodes)));

            CreateMap<RuleFailure, RuleFailureDto>();

            CreateMap<TransactionCheckResult, ProcessTransactionResponse>()
                .ForMember(d => d.Decision,
                    o => o.MapFrom(s => RiskDecisionNames.ToWireFormat(
                        s.FraudDecision == null ? RiskDecisionEnum.Normal : s.FraudDecision.Decision)))
                .ForMember(d => d.RiskScore,
                    o => o.MapFrom(s => s.FraudDecision == null ? 0 : s.FraudDecision.FinalRiskScore))
                .ForMember(d => d.CardRiskScore,
                    o => o.MapFrom(s => s.FraudDecision == null ? 0 : s.FraudDecision.CardRiskScore))
                .ForMember(d => d.MerchantRiskScore,
                    o => o.MapFrom(s => s.FraudDecision == null ? 0 : s.FraudDecision.MerchantRiskScore))
                .ForMember(d => d.RawRuleScore,
                    o => o.MapFrom(s => s.FraudDecision == null ? 0 : s.FraudDecision.RawRuleScore))
                .ForMember(d => d.TotalBonusScore,
                    o => o.MapFrom(s => s.FraudDecision == null ? 0 : s.FraudDecision.TotalBonusScore))
                .ForMember(d => d.TotalTrustDiscount,
                    o => o.MapFrom(s => s.FraudDecision == null ? 0 : s.FraudDecision.TotalTrustDiscount))
                .ForMember(d => d.RequiresAdditionalVerification,
                    o => o.MapFrom(s => s.FraudDecision != null
                                        && s.FraudDecision.Decision == RiskDecisionEnum.EkDogrulama))
                .ForMember(d => d.TriggeredRules,
                    o => o.MapFrom(s => s.FraudDecision == null
                        ? new List<TriggeredRule>()
                        : s.FraudDecision.TriggeredRules.ToList()))
                .ForMember(d => d.AppliedCombinations,
                    o => o.MapFrom(s => s.FraudDecision == null
                        ? new List<AppliedCombination>()
                        : s.FraudDecision.AppliedCombinations.ToList()))
                .ForMember(d => d.TrustFactors,
                    o => o.MapFrom(s => s.FraudDecision == null
                        ? new List<string>()
                        : s.FraudDecision.TrustFactors.ToList()))
                .ForMember(d => d.RuleFailures,
                    o => o.MapFrom(s => s.FraudDecision == null
                        ? new List<RuleFailure>()
                        : s.FraudDecision.Failures.ToList()));
        }
    }
}

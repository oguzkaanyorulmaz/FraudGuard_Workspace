using AutoMapper;
using FraudGuard.Application.DTOs;
using FraudGuard.Application.DTOs.RuleManagement;
using FraudGuard.Application.Interfaces;
using FraudGuard.Domain.Interfaces.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FraudGuard.Application.Services
{
    public class RuleManagementAppService : IRuleManagementAppService
    {
        private readonly IFraudRuleRepository _fraudRuleRepository;
        private readonly IMapper _mapper;

        public RuleManagementAppService(IFraudRuleRepository fraudRuleRepository, IMapper mapper)
        {
            _fraudRuleRepository = fraudRuleRepository;
            _mapper = mapper;
        }

        public async Task<ResponseDTO<List<GetActiveRulesResponse>>> GetActiveRulesAsync()
        {
            var rules = await _fraudRuleRepository.GetAllActiveRulesAsync();
            var responseList = _mapper.Map<List<GetActiveRulesResponse>>(rules);
            return ResponseDTO<List<GetActiveRulesResponse>>.Success(responseList);
        }
    }
}
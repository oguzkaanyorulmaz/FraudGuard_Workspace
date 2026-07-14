using System.Collections.Generic;
using System.Threading.Tasks;
using FraudGuard.Application.DTOs;
using FraudGuard.Application.DTOs.RuleManagement;

namespace FraudGuard.Application.Interfaces
{
    public interface IRuleManagementAppService
    {
        Task<ResponseDTO<List<GetActiveRulesResponse>>> GetActiveRulesAsync();
    }
}
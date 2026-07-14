using FraudGuard.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FraudGuard.Domain.Interfaces.Repositories
{
    public interface IFraudRuleRepository
    {
        Task<EFraudRule> GetByCodeAsync(string ruleCode);
        Task<List<EFraudRule>> GetAllActiveRulesAsync();
    }
}
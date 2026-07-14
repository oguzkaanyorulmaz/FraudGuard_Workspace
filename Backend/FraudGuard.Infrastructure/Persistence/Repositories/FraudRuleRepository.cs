using FraudGuard.Domain.Entities;
using FraudGuard.Domain.Interfaces.Repositories;
using FraudGuard.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FraudGuard.Infrastructure.Persistence.Repositories
{
    public class FraudRuleRepository : IFraudRuleRepository
    {
        private readonly FraudGuardDbContext _context;

        public FraudRuleRepository(FraudGuardDbContext context)
        {
            _context = context;
        }

        public async Task<EFraudRule> GetByCodeAsync(string ruleCode)
        {
            return await _context.FraudRules.FirstOrDefaultAsync(r => r.RuleCode == ruleCode);
        }

        public async Task<List<EFraudRule>> GetAllActiveRulesAsync()
        {
            return await _context.FraudRules.Where(r => r.IsActive).ToListAsync();
        }
    }
}
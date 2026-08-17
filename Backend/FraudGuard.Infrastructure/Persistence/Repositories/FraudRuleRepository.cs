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
            // Motor her işlemde bu listeyi okur; önbelleğe alınmaz ki veritabanına eklenen
            // kural bir sonraki işlemde devreye girsin. Takip edilmesine gerek yok.
            return await _context.FraudRules
                .AsNoTracking()
                .Where(r => r.IsActive)
                .ToListAsync();
        }

        public async Task<List<EFraudRule>> GetAllAsync()
        {
            return await _context.FraudRules
                .AsNoTracking()
                .OrderBy(r => r.RuleId)
                .ToListAsync();
        }

        public async Task<bool> ExistsByCodeAsync(string ruleCode)
        {
            return await _context.FraudRules.AnyAsync(r => r.RuleCode == ruleCode);
        }

        public async Task AddAsync(EFraudRule rule)
        {
            await _context.FraudRules.AddAsync(rule);
        }
    }
}

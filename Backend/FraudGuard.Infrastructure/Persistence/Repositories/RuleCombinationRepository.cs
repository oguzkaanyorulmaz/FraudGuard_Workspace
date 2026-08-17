using FraudGuard.Domain.Entities;
using FraudGuard.Domain.Interfaces.Repositories;
using FraudGuard.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FraudGuard.Infrastructure.Persistence.Repositories
{
    public class RuleCombinationRepository : IRuleCombinationRepository
    {
        private readonly FraudGuardDbContext _context;

        public RuleCombinationRepository(FraudGuardDbContext context)
        {
            _context = context;
        }

        public async Task<List<ERuleCombination>> GetAllActiveAsync()
        {
            return await _context.RuleCombinations
                .AsNoTracking()
                .Where(c => c.IsActive)
                .ToListAsync();
        }
    }
}

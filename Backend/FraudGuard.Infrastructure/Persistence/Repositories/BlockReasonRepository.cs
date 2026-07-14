using FraudGuard.Domain.Entities;
using FraudGuard.Domain.Interfaces.Repositories;
using FraudGuard.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FraudGuard.Infrastructure.Persistence.Repositories
{
    public class BlockReasonRepository : IBlockReasonRepository
    {
        private readonly FraudGuardDbContext _context;

        public BlockReasonRepository(FraudGuardDbContext context)
        {
            _context = context;
        }

        public async Task<List<EBlockReason>> GetAllAsync()
        {
            return await _context.BlockReasons.AsNoTracking().ToListAsync();
        }

        public async Task<EBlockReason> GetByCodeAsync(string reasonCode)
        {
            return await _context.BlockReasons.FirstOrDefaultAsync(b => b.ReasonCode == reasonCode);
        }
    }
}
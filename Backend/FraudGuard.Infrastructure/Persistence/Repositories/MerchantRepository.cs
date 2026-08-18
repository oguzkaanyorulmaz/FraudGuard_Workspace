using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FraudGuard.Domain.Entities;
using FraudGuard.Domain.Interfaces.Repositories;
using FraudGuard.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace FraudGuard.Infrastructure.Persistence.Repositories
{
    public class MerchantRepository : IMerchantRepository
    {
        private readonly FraudGuardDbContext _context;

        public MerchantRepository(FraudGuardDbContext context)
        {
            _context = context;
        }

        public async Task<EMerchant?> GetByIdAsync(string merchantId)
        {
            if (string.IsNullOrWhiteSpace(merchantId))
                return null;

            return await _context.Merchants
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.MerchantId == merchantId);
        }

        public async Task<List<EMerchant>> GetAllActiveAsync()
        {
            return await _context.Merchants
                .AsNoTracking()
                .Where(m => m.IsActive)
                .OrderBy(m => m.MerchantName)
                .ToListAsync();
        }
    }
}

using FraudGuard.Domain.Entities;
using FraudGuard.Domain.Interfaces.Repositories;
using FraudGuard.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FraudGuard.Infrastructure.Persistence.Repositories
{
    public class ReferenceDataRepository : IReferenceDataRepository
    {
        private readonly FraudGuardDbContext _context;

        public ReferenceDataRepository(FraudGuardDbContext context)
        {
            _context = context;
        }

        public async Task<List<EBinRange>> GetActiveBinRangesAsync() =>
            await _context.BinRanges.AsNoTracking().Where(b => b.IsActive).ToListAsync();

        public async Task<List<EReferenceListEntry>> GetActiveListEntriesAsync() =>
            await _context.ReferenceListEntries.AsNoTracking().Where(e => e.IsActive).ToListAsync();
    }
}

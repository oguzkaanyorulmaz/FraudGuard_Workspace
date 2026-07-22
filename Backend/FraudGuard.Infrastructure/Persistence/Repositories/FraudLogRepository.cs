using FraudGuard.Domain.Entities;
using FraudGuard.Domain.Interfaces.Repositories;
using FraudGuard.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FraudGuard.Infrastructure.Persistence.Repositories
{
    public class FraudLogRepository : IFraudLogRepository
    {
        private readonly FraudGuardDbContext _context;

        public FraudLogRepository(FraudGuardDbContext context)
        {
            _context = context;
        }

        public async Task<List<EFraudLog>> GetUnresolvedLogsAsync()
        {
            return await _context.FraudLogs
                .Include(f => f.FraudRule)
                .Include(f => f.CreditCardTransaction)
                    .ThenInclude(t => t.CreditCard)
                .Include(f => f.DebitCardTransaction)
                    .ThenInclude(t => t.DebitCard)
                .Include(f => f.TransferTransaction)
                .Where(f => string.IsNullOrEmpty(f.AdminAction))
                .ToListAsync();
        }

        public async Task AddAsync(EFraudLog log)
        {
            await _context.FraudLogs.AddAsync(log);
        }

        public async Task<EFraudLog> GetByIdAsync(int logId)
        {
            return await _context.FraudLogs
                .Include(f => f.CreditCardTransaction)
                .Include(f => f.DebitCardTransaction)
                .Include(f => f.TransferTransaction)
                .FirstOrDefaultAsync(l => l.LogId == logId);
        }

        public async Task UpdateAsync(EFraudLog fraudLog)
        {
            _context.FraudLogs.Update(fraudLog);
            await Task.CompletedTask;
        }

        public async Task<EFraudLog> GetLogWithDetailsAsync(int logId)
        {
            return await _context.FraudLogs
                .Include(f => f.FraudRule)
                .Include(f => f.CreditCardTransaction)
                    .ThenInclude(t => t.CreditCard)
                        .ThenInclude(c => c.Customer)
                .Include(f => f.DebitCardTransaction)
                    .ThenInclude(t => t.DebitCard)
                        .ThenInclude(d => d.Customer)
                .Include(f => f.CreditCardTransaction)
                    .ThenInclude(t => t.TransactionType)
                .Include(f => f.DebitCardTransaction)
                    .ThenInclude(t => t.TransactionType)
                .Include(f => f.TransferTransaction)
                .FirstOrDefaultAsync(f => f.LogId == logId);
        }

        public async Task<List<EFraudLog>> GetResolvedLogsAsync()
        {
            return await _context.FraudLogs
                .Include(f => f.FraudRule)
                .Include(f => f.CreditCardTransaction)
                    .ThenInclude(t => t.CreditCard)
                .Include(f => f.DebitCardTransaction)
                    .ThenInclude(t => t.DebitCard)
                .Include(f => f.TransferTransaction)
                .Where(f => !string.IsNullOrEmpty(f.AdminAction)) 
                .OrderByDescending(f => f.LogId)
                .ToListAsync();
        }
    }
}
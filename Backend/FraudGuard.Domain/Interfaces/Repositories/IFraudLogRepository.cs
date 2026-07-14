using FraudGuard.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FraudGuard.Domain.Interfaces.Repositories
{
    public interface IFraudLogRepository
    {
        Task AddAsync(EFraudLog log);
        Task<List<EFraudLog>> GetUnresolvedLogsAsync();
        Task<EFraudLog> GetByIdAsync(int logId);
        Task UpdateAsync(EFraudLog fraudLog);
        Task<EFraudLog> GetLogWithDetailsAsync(int logId);
        Task<List<EFraudLog>> GetResolvedLogsAsync();

    }
}
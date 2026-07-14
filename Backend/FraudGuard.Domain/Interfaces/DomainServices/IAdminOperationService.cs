using FraudGuard.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FraudGuard.Domain.Interfaces.DomainServices
{
    public interface IAdminOperationService
    {
        Task<List<EFraudLog>> GetUnresolvedLogsAsync();
        Task<bool> ResolveFraudLogAsync(int logId, string adminAction, string adminNote, int? blockReasonId = null);
    }
}
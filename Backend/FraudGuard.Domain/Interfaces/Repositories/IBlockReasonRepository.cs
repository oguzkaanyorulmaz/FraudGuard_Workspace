using FraudGuard.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FraudGuard.Domain.Interfaces.Repositories
{
    public interface IBlockReasonRepository
    {
        Task<List<EBlockReason>> GetAllAsync();
        Task<EBlockReason> GetByCodeAsync(string reasonCode);
    }
}
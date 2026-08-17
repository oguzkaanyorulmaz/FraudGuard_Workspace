using System.Collections.Generic;
using System.Threading.Tasks;
using FraudGuard.Domain.Entities;

namespace FraudGuard.Domain.Interfaces.Repositories
{
    public interface IRuleCombinationRepository
    {
        Task<List<ERuleCombination>> GetAllActiveAsync();
    }
}

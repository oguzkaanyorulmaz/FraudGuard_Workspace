using System.Collections.Generic;
using System.Threading.Tasks;
using FraudGuard.Domain.Entities;

namespace FraudGuard.Domain.Interfaces.Repositories
{
    public interface IReferenceDataRepository
    {
        Task<List<EBinRange>> GetActiveBinRangesAsync();

        Task<List<EReferenceListEntry>> GetActiveListEntriesAsync();
    }
}

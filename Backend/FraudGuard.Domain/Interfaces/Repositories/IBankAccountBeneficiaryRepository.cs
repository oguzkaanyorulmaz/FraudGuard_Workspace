using FraudGuard.Domain.Entities;
using System.Threading.Tasks;

namespace FraudGuard.Domain.Interfaces.Repositories
{
    public interface IBankAccountBeneficiaryRepository
    {
        Task<bool> AnyAsync(int customerId, string receiverIBAN);
        Task AddAsync(EBankAccountBeneficiary beneficiary);
    }
}

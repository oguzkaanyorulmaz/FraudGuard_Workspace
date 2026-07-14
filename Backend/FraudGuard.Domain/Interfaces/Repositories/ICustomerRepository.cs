using FraudGuard.Domain.Entities;
using System.Threading.Tasks;

namespace FraudGuard.Domain.Interfaces.Repositories
{
    public interface ICustomerRepository
    {
        Task<ECustomer> GetByIdAsync(int customerId);
        Task<ECustomer> GetByIdentityNumberAsync(string identityNumber);
    }
}
using FraudGuard.Domain.Entities;
using FraudGuard.Domain.Interfaces.Repositories;
using FraudGuard.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace FraudGuard.Infrastructure.Persistence.Repositories
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly FraudGuardDbContext _context;

        public CustomerRepository(FraudGuardDbContext context)
        {
            _context = context;
        }

        public async Task<ECustomer> GetByIdAsync(int customerId)
        {
            return await _context.Customers.FindAsync(customerId);
        }

        public async Task<ECustomer> GetByIdentityNumberAsync(string identityNumber)
        {
            return await _context.Customers.FirstOrDefaultAsync(c => c.IdentityNumber == identityNumber);
        }
    }
}
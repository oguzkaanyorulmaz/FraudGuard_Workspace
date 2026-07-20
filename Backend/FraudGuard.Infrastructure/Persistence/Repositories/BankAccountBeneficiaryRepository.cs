using FraudGuard.Domain.Entities;
using FraudGuard.Domain.Interfaces.Repositories;
using FraudGuard.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace FraudGuard.Infrastructure.Persistence.Repositories
{
    public class BankAccountBeneficiaryRepository : IBankAccountBeneficiaryRepository
    {
        private readonly FraudGuardDbContext _context;

        public BankAccountBeneficiaryRepository(FraudGuardDbContext context)
        {
            _context = context;
        }

        public async Task<bool> AnyAsync(int customerId, string receiverIBAN)
        {
            return await _context.BankAccountBeneficiaries
                .AnyAsync(b => b.CustomerId == customerId && b.ReceiverIBAN == receiverIBAN);
        }

        public async Task AddAsync(EBankAccountBeneficiary beneficiary)
        {
            await _context.BankAccountBeneficiaries.AddAsync(beneficiary);
        }
    }
}

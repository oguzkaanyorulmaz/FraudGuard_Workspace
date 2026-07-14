using FraudGuard.Domain.Entities;
using FraudGuard.Domain.Interfaces.Repositories;
using FraudGuard.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace FraudGuard.Infrastructure.Persistence.Repositories
{
    public class CreditCardRepository : ICreditCardRepository
    {
        private readonly FraudGuardDbContext _context;

        public CreditCardRepository(FraudGuardDbContext context)
        {
            _context = context;
        }

        public async Task<ECreditCard> GetByCardNumberAsync(string cardNumber)
        {
            return await _context.CreditCards.FirstOrDefaultAsync(c => c.CardNumber == cardNumber);
        }

        public async Task<ECreditCard> GetByIdAsync(int cardId)
        {
            return await _context.CreditCards.FirstOrDefaultAsync(c => c.CardId == cardId);
        }

        public async Task UpdateAsync(ECreditCard creditCard)
        {
            _context.CreditCards.Update(creditCard);
            await Task.CompletedTask;
        }
    }
}
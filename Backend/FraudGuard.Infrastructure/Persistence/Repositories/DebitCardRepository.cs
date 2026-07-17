using FraudGuard.Domain.Entities;
using FraudGuard.Domain.Interfaces.Repositories;
using FraudGuard.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace FraudGuard.Infrastructure.Persistence.Repositories
{
    public class DebitCardRepository : IDebitCardRepository
    {
        private readonly FraudGuardDbContext _context;

        public DebitCardRepository(FraudGuardDbContext context)
        {
            _context = context;
        }

        public async Task<EDebitCard> GetByCardNumberAsync(string cardNumber)
        {
            return await _context.DebitCards.FirstOrDefaultAsync(d => d.CardNumber == cardNumber);
        }

        public async Task<EDebitCard> GetByIBANAsync(string iban)
        {
            return await _context.DebitCards.FirstOrDefaultAsync(d => d.IBAN == iban);
        }

        public async Task<EDebitCard> GetByIdAsync(int cardId)
        {
            return await _context.DebitCards.FirstOrDefaultAsync(d => d.CardId == cardId);
        }

        public async Task UpdateAsync(EDebitCard debitCard)
        {
            _context.DebitCards.Update(debitCard);
            await Task.CompletedTask;
        }

        public async Task AddAsync(EDebitCard debitCard)
        {
            await _context.DebitCards.AddAsync(debitCard);
        }
    }
}

using FraudGuard.Domain.Entities;
using System.Threading.Tasks;

namespace FraudGuard.Domain.Interfaces.Repositories
{
    public interface IDebitCardRepository
    {
        Task<EDebitCard> GetByCardNumberAsync(string cardNumber);
        Task<EDebitCard> GetByIBANAsync(string iban);
        Task<EDebitCard> GetByIdAsync(int cardId);
        Task UpdateAsync(EDebitCard debitCard);
        Task AddAsync(EDebitCard debitCard);
    }
}

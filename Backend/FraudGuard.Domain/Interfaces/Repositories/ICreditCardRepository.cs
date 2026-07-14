using FraudGuard.Domain.Entities;
using System.Threading.Tasks;

namespace FraudGuard.Domain.Interfaces.Repositories
{
    public interface ICreditCardRepository
    {
        Task<ECreditCard> GetByCardNumberAsync(string cardNumber);
        Task<ECreditCard> GetByIdAsync(int cardId);
        Task UpdateAsync(ECreditCard creditCard);
    }
}
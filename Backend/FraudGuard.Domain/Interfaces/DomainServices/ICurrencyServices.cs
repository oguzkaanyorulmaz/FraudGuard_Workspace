using System.Threading.Tasks;

namespace FraudGuard.Domain.Interfaces.DomainServices
{
    public interface ICurrencyService
    {
        Task<decimal> ConvertToTryAsync(decimal amount, string fromCurrency);
    }
}

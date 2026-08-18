using System.Threading.Tasks;

namespace FraudGuard.Domain.Interfaces.Abstractions
{
    public interface ICurrencyService
    {
        Task<decimal> ConvertToTryAsync(decimal amount, string fromCurrency);
    }
}

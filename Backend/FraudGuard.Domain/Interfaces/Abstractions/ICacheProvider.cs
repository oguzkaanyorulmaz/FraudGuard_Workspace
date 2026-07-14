using System;
using System.Threading.Tasks;

namespace FraudGuard.Domain.Interfaces.Abstractions
{
    public interface ICacheProvider
    {
        Task<T> GetAsync<T>(string key);
        Task SetAsync<T>(string key, T value, TimeSpan? expirationTime = null);
        Task RemoveAsync(string key);
    }
}
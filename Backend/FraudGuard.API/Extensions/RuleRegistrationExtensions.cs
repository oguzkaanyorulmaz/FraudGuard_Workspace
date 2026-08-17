using Microsoft.Extensions.DependencyInjection;

namespace FraudGuard.API.Extensions
{
    public static class RuleRegistrationExtensions
    {
        public static IServiceCollection AddFraudRules(this IServiceCollection services)
        {
            // Tüm kurallar dinamik kural motoru (DynamicExpresso) üzerinden çalışır.
            // Kod tabanlı kural sınıfları kaldırılmıştır.
            return services;
        }
    }
}


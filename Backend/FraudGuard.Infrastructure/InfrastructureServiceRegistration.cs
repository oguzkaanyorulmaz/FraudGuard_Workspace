using FraudGuard.Domain.Interfaces.Abstractions;
using FraudGuard.Domain.Interfaces.Repositories;
using FraudGuard.Infrastructure.Cache;
using FraudGuard.Infrastructure.Persistence;
using FraudGuard.Infrastructure.Persistence.Contexts;
using FraudGuard.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FraudGuard.Infrastructure
{
    public static class InfrastructureServiceRegistration
    {

        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<FraudGuardDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // 3. Repository Kayıtları (Sözleşme -> Gerçek Kod eşleşmeleri)
            services.AddScoped<ICustomerRepository, CustomerRepository>();
            services.AddScoped<ICreditCardRepository, CreditCardRepository>();
            services.AddScoped<ITransactionRepository, TransactionRepository>();
            services.AddScoped<IFraudRuleRepository, FraudRuleRepository>();
            services.AddScoped<IFraudLogRepository, FraudLogRepository>();
            services.AddScoped<IBlockReasonRepository, BlockReasonRepository>();

            // 4. Cache Kaydı (MemoryCache yerine Redis'e geçiş)
// services.AddMemoryCache();
// services.AddSingleton<ICacheProvider, MemoryCacheProvider>();

services.AddStackExchangeRedisCache(options => options.Configuration = configuration.GetConnectionString("RedisConnection"));
services.AddSingleton<ICacheProvider, RedisCacheProvider>();


            return services;
        }
    }
}
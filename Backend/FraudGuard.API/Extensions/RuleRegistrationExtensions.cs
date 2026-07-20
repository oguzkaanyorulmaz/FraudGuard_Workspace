using FraudGuard.Domain.Interfaces.Rules;
using FraudGuard.Domain.Services.Rules;
using Microsoft.Extensions.DependencyInjection;

namespace FraudGuard.API.Extensions
{
    public static class RuleRegistrationExtensions
    {
        public static IServiceCollection AddFraudRules(this IServiceCollection services)
        {
            services.AddScoped<IFraudRule, VelocityRule>();
            services.AddScoped<IFraudRule, ImpossibleTravelRule>();
            services.AddScoped<IFraudRule, AnomalousTimeRule>();
            services.AddScoped<IFraudRule, CardTestingRule>();
            services.AddScoped<IFraudRule, BruteForceRule>();
            services.AddScoped<IFraudRule, CrossBorderRule>();
            services.AddScoped<IFraudRule, HighRiskMccRule>();
            services.AddScoped<IFraudRule, MaxOutAttemptRule>();
            services.AddScoped<IFraudRule, CurrencyMismatchRule>();
            services.AddScoped<IFraudRule, ConsecutiveRefundsRule>();
            services.AddScoped<IFraudRule, LateVoidRule>();
            services.AddScoped<IFraudRule, HighValueRefundVoidRule>();
            services.AddScoped<IFraudRule, SmurfingRule>();
            services.AddScoped<IFraudRule, WalletCashoutRule>();
            services.AddScoped<IFraudRule, MultiSourceFundingRule>();
            services.AddScoped<IFraudRule, CrossBorderTransferRule>();
            services.AddScoped<IFraudRule, AccountDrainRule>();
            services.AddScoped<IFraudRule, NewBeneficiaryTransferRule>();
            services.AddScoped<IFraudRule, SuspiciousDescriptionRule>();
            services.AddScoped<IFraudRule, HighRiskReceiverRule>();
            services.AddScoped<IFraudRule, MultiSenderToSingleReceiverRule>();
            services.AddScoped<IFraudRule, ReceiverBalanceAnomalyRule>();

            return services;
        }
    }
}

using FraudGuard.Domain.Interfaces.Entities;
using FraudGuard.Domain.DomainObjects.TransactionProcessing;
using FraudGuard.Domain.Entities;
using FraudGuard.Domain.Interfaces.Rules;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FraudGuard.Domain.Services.Rules
{
    public class AnomalousTimeRule : IFraudRule
    {
        public string RuleCode => "ANOMALOUS_TIME";
        public string RuleName => "Zaman ve Tutar Kuralı (Anomalous Behavior - Herkes için)";

        public Task<(bool IsSuspicious, string? Reason)> EvaluateAsync(ProcessTransactionInput input, List<ITransaction> history)
        {
            var turkeyZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
var turkeyTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, turkeyZone);
int currentHour = turkeyTime.Hour; 
            if (currentHour >= 2 && currentHour <= 5 && input.Amount >= 100000)
            {
                return Task.FromResult((true, (string?)"Gece 02:00 - 05:00 saatleri arasında 100.000 TL ve üzeri yüksek tutarlı harcama/transfer denemesi."));
            }

            return Task.FromResult((false, (string?)null));
        }
    }
}
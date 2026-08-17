using System;
using FraudGuard.Domain.Interfaces.Abstractions;
using Microsoft.Extensions.Logging;

namespace FraudGuard.Infrastructure.Diagnostics
{
    /// <summary>
    /// <see cref="IRuleDiagnostics"/>'in ILogger tabanlı implementasyonu.
    /// </summary>
    public class RuleDiagnostics : IRuleDiagnostics
    {
        private readonly ILogger<RuleDiagnostics> _logger;

        public RuleDiagnostics(ILogger<RuleDiagnostics> logger)
        {
            _logger = logger;
        }

        public void RuleEvaluationFailed(string ruleCode, string? expression, Exception exception)
        {
            _logger.LogError(
                exception,
                "KURAL DEĞERLENDİRİLEMEDİ — kod: {RuleCode} | ifade: {Expression} | hata: {Error}. " +
                "Bu kural atlandı, işlem akışı etkilenmedi.",
                ruleCode,
                expression ?? "(kod tabanlı)",
                exception.Message);
        }
    }
}

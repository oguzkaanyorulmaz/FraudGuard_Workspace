using System;

namespace FraudGuard.Domain.Interfaces.Abstractions
{
    /// <summary>
    /// Kural motorunun teşhis kanalı. Domain'in loglama kütüphanesine bağlanmaması için
    /// soyutlanmıştır; implementasyon Infrastructure'dadır.
    /// </summary>
    public interface IRuleDiagnostics
    {
        /// <summary>
        /// Bir kural değerlendirilemediğinde çağrılır. Bozuk ifade, eksik alan veya
        /// çalışma anı hatası bu yolla görünür hale gelir.
        /// </summary>
        void RuleEvaluationFailed(string ruleCode, string? expression, Exception exception);
    }
}

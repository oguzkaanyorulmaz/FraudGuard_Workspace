using System.Collections.Generic;
using FraudGuard.Domain.DomainObjects.FraudEvaluation;
using FraudGuard.Domain.DomainObjects.TransactionProcessing;
using FraudGuard.Domain.Entities;

namespace FraudGuard.Domain.Interfaces.DomainServices
{
    /// <summary>
    /// Aktif kuralların tamamını çalıştırıp tetiklenenleri ve değerlendirilemeyenleri döner.
    /// İlk eşleşmede durmaz; kümülatif skorlama için tüm kurallar değerlendirilir.
    /// <para>
    /// Girdi olarak zenginleştirilmiş input alır; kendisi veriye erişmez.
    /// </para>
    /// </summary>
    public interface IDynamicRuleEngine
    {
        RuleEvaluationOutcome Evaluate(
            ProcessTransactionInput input,
            IReadOnlyList<EFraudRule> activeRules);
    }
}

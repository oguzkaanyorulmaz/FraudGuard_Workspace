using System.Collections.Generic;
using System.Threading.Tasks;
using FraudGuard.Domain.DomainObjects.FraudEvaluation;
using FraudGuard.Domain.DomainObjects.TransactionProcessing;
using FraudGuard.Domain.Entities;
using FraudGuard.Domain.Interfaces.Entities;

namespace FraudGuard.Domain.Interfaces.DomainServices
{
    /// <summary>
    /// Aktif kuralların tamamını çalıştırıp tetiklenenleri ve değerlendirilemeyenleri döner.
    /// İlk eşleşmede durmaz; kümülatif skorlama için tüm kurallar değerlendirilir.
    /// </summary>
    public interface IDynamicRuleEngine
    {
        Task<RuleEvaluationOutcome> EvaluateAsync(
            ProcessTransactionInput input,
            IReadOnlyList<EFraudRule> activeRules,
            List<ITransaction> history);
    }
}

using System.Collections.Generic;
using FraudGuard.Domain.DomainObjects.FraudEvaluation;

namespace FraudGuard.Domain.Interfaces.DomainServices
{
    /// <summary>
    /// Kural sonuçlarını nihai risk skoruna ve karara dönüştüren iş kuralı.
    /// Saf hesaplamadır: repository'ye, saate veya dış kaynağa erişmez.
    /// </summary>
    public interface IRiskScoringService
    {
        FraudDecisionResult BuildDecision(
            RuleEvaluationOutcome outcome,
            IReadOnlyList<AppliedCombination> appliedCombinations,
            TrustAssessment trust);
    }
}

using System.Collections.Generic;
using FraudGuard.Domain.DomainObjects.FraudEvaluation;
using FraudGuard.Domain.Entities;

namespace FraudGuard.Domain.Interfaces.DomainServices
{
    /// <summary>
    /// Birlikte tetiklenen kural örüntülerini tespit edip bonus puan üretir.
    /// </summary>
    public interface ICombinationEngine
    {
        IReadOnlyList<AppliedCombination> Evaluate(
            IReadOnlyList<TriggeredRule> triggeredRules,
            IReadOnlyList<ERuleCombination> combinations);
    }
}

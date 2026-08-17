using FraudGuard.Domain.DomainObjects.FraudEvaluation;

namespace FraudGuard.Domain.Interfaces.DomainServices
{
    /// <summary>
    /// Güven geçmişine göre risk indirimi hesaplar. Saf hesaplama yapar, veri erişimi içermez.
    /// </summary>
    public interface ITrustScoreService
    {
        TrustAssessment Evaluate(TrustContext context);
    }
}

using System.Collections.Generic;

namespace FraudGuard.Domain.DomainObjects.FraudEvaluation
{
    /// <summary>
    /// Değerlendirilemeyen bir kuralın kaydı.
    /// <para>
    /// Bozuk bir kural ödeme akışını düşürmemelidir; ancak sessizce atlanması da kabul edilemez —
    /// yanlış yazılmış bir kural fark edilmeden aylarca ölü kalabilir. Bu nesne, atlanan kuralı
    /// hem loga hem de karar sonucuna taşır.
    /// </para>
    /// </summary>
    public sealed class RuleFailure
    {
        public required string RuleCode { get; init; }
        public string? Expression { get; init; }
        public required string Error { get; init; }
    }

    /// <summary>
    /// Kural motorunun tek turluk çıktısı: tetiklenenler ve değerlendirilemeyenler.
    /// </summary>
    public sealed class RuleEvaluationOutcome
    {
        public IReadOnlyList<TriggeredRule> Triggered { get; init; } = new List<TriggeredRule>();
        public IReadOnlyList<RuleFailure> Failures { get; init; } = new List<RuleFailure>();

        public static RuleEvaluationOutcome Empty => new();
    }
}

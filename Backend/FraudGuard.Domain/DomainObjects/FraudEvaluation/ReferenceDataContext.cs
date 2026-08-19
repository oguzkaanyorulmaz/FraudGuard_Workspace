using System;
using System.Collections.Generic;
using FraudGuard.Domain.Entities;

namespace FraudGuard.Domain.DomainObjects.FraudEvaluation
{
    /// <summary>
    /// Kural değerlendirmesi için hazırlanmış referans verisi.
    /// <para>
    /// Enricher saf kalsın diye veriyi orkestratör toplar ve bu nesneyle geçer.
    /// Aramalar sıcak yolda çalıştığı için sözlük/küme olarak verilir.
    /// </para>
    /// </summary>
    public sealed class ReferenceDataContext
    {
        public IReadOnlyDictionary<string, EBinRange> BinRanges { get; init; } =
            new Dictionary<string, EBinRange>(StringComparer.OrdinalIgnoreCase);

        public IReadOnlySet<string> BlockedCountries { get; init; } =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public IReadOnlySet<string> RiskyCountries { get; init; } =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public IReadOnlySet<string> BlockedSchemes { get; init; } =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public IReadOnlySet<string> PinlessBlockedMccs { get; init; } =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public IReadOnlySet<string> JewelryMccs { get; init; } =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Referans verisi yüklenemediğinde kullanılan boş bağlam.</summary>
        public static ReferenceDataContext Empty { get; } = new();
    }
}

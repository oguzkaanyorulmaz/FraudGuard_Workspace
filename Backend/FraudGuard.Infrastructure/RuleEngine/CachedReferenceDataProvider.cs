using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FraudGuard.Domain.Common.Constants;
using FraudGuard.Domain.DomainObjects.FraudEvaluation;
using FraudGuard.Domain.Interfaces.Abstractions;
using FraudGuard.Domain.Interfaces.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace FraudGuard.Infrastructure.RuleEngine
{
    /// <summary>
    /// Referans verisini süreç belleğinde, hazır arama yapılarında tutar ve süresi dolunca yeniler.
    /// <para>
    /// Singleton'dır: hazırlanan sözlük ve kümeler tüm isteklerce paylaşılır. Veriyi okumak için
    /// scoped bir repository gerektiğinden <see cref="IServiceScopeFactory"/> ile geçici kapsam açılır.
    /// </para>
    /// <para>
    /// Yenileme sırasında hata olursa <b>eldeki veri korunur</b>. Referans verisi okunamadı diye
    /// yaptırım kurallarının sessizce devre dışı kalması, ödemeyi durdurmaktan daha tehlikelidir.
    /// </para>
    /// </summary>
    public class CachedReferenceDataProvider : IReferenceDataProvider
    {
        /// <summary>
        /// Yenileme aralığı. Referans verisi nadiren değişir; liste güncellemesinin devreye
        /// girmesi için beklenecek en uzun süredir.
        /// </summary>
        private static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(10);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly SemaphoreSlim _refreshGate = new(1, 1);

        private ReferenceDataContext _current = ReferenceDataContext.Empty;
        private DateTime _loadedAtUtc = DateTime.MinValue;
        private bool _loadedAtLeastOnce;

        public CachedReferenceDataProvider(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async Task<ReferenceDataContext> GetAsync()
        {
            if (!IsStale())
                return _current;

            await _refreshGate.WaitAsync();
            try
            {
                // Kilidi beklerken başka bir istek yenilemiş olabilir.
                if (!IsStale())
                    return _current;

                await RefreshAsync();
            }
            finally
            {
                _refreshGate.Release();
            }

            return _current;
        }

        private bool IsStale() =>
            !_loadedAtLeastOnce || (DateTime.UtcNow - _loadedAtUtc) > RefreshInterval;

        private async Task RefreshAsync()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IReferenceDataRepository>();

                var bins = await repository.GetActiveBinRangesAsync();
                var entries = await repository.GetActiveListEntriesAsync();

                IReadOnlySet<string> Of(string listType) => entries
                    .Where(e => string.Equals(e.ListType, listType, StringComparison.OrdinalIgnoreCase))
                    .Select(e => e.Value)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                _current = new ReferenceDataContext
                {
                    BinRanges = bins.ToDictionary(b => b.BinPrefix, b => b, StringComparer.OrdinalIgnoreCase),
                    BlockedCountries = Of(ReferenceListTypes.BlockedCountry),
                    RiskyCountries = Of(ReferenceListTypes.RiskyCountry),
                    BlockedSchemes = Of(ReferenceListTypes.BlockedScheme),
                    PinlessBlockedMccs = Of(ReferenceListTypes.PinlessBlockedMcc),
                    JewelryMccs = Of(ReferenceListTypes.JewelryMcc)
                };

                _loadedAtUtc = DateTime.UtcNow;
                _loadedAtLeastOnce = true;
            }
            catch
            {
                // Elde veri varsa onunla devam edilir; yoksa boş bağlam döner ve referans
                // tabanlı kurallar tetiklenmez. Her iki durumda da ödeme akışı durmaz.
                if (_loadedAtLeastOnce)
                    _loadedAtUtc = DateTime.UtcNow;
            }
        }
    }
}

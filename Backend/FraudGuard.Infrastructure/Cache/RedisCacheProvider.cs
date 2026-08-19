using FraudGuard.Domain.Interfaces.Abstractions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FraudGuard.Infrastructure.Cache
{
    /// <summary>
    /// Redis tabanlı önbellek. Süreç dışında durduğu için birden fazla API örneği
    /// aynı önbelleği paylaşır — bir kartın bloke edilmesi diğer örnekte de anında görülür.
    /// </summary>
    public class RedisCacheProvider : ICacheProvider
    {
        /// <summary>
        /// EF varlıkları çift yönlü navigasyon taşır (işlem → kart → işlemler).
        /// <see cref="ReferenceHandler.IgnoreCycles"/> olmadan serileştirme döngüye girip
        /// hata verirdi. Alan adları büyük/küçük harf duyarsız okunur ki ileride
        /// serileştirme ayarı değişse bile eldeki kayıtlar okunabilsin.
        /// </summary>
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private readonly IDistributedCache _distributedCache;
        private readonly ILogger<RedisCacheProvider> _logger;

        public RedisCacheProvider(IDistributedCache distributedCache, ILogger<RedisCacheProvider> logger)
        {
            _distributedCache = distributedCache;
            _logger = logger;
        }

        /// <summary>
        /// Önbellekten okur. Redis erişilemezse <c>default</c> döner: önbellek hatası
        /// ödeme akışını durdurmamalı, yalnızca isabetsizliğe yol açmalıdır.
        /// </summary>
        public async Task<T> GetAsync<T>(string key)
        {
            try
            {
                var cached = await _distributedCache.GetStringAsync(key);

                return string.IsNullOrEmpty(cached)
                    ? default!
                    : JsonSerializer.Deserialize<T>(cached, SerializerOptions)!;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Önbellek okunamadı ({Key}); veri kaynağa gidilerek alınacak.", key);
                return default!;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expirationTime = null)
        {
            try
            {
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expirationTime ?? TimeSpan.FromHours(1)
                };

                await _distributedCache.SetStringAsync(
                    key, JsonSerializer.Serialize(value, SerializerOptions), options);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Önbelleğe yazılamadı ({Key}); işlem etkilenmedi.", key);
            }
        }

        /// <summary>
        /// Kaydı siler. Buradaki hata sessiz geçilemez: silinemeyen bir kayıt, bloke edilmiş
        /// kartın bir süre daha geçerli görünmesi demektir. Bu yüzden hata seviyesinde loglanır.
        /// </summary>
        public async Task RemoveAsync(string key)
        {
            try
            {
                await _distributedCache.RemoveAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Önbellek kaydı silinemedi ({Key}); bayat veri süresi dolana kadar okunabilir.", key);
            }
        }
    }
}

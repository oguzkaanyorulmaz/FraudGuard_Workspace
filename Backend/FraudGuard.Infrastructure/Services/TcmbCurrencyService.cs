using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Xml.Linq;
using System.Threading.Tasks;
using FraudGuard.Domain.Interfaces.DomainServices;
using FraudGuard.Domain.Interfaces.Abstractions;

namespace FraudGuard.Infrastructure.Services
{
    public class TcmbCurrencyService : ICurrencyService
    {
        private readonly ICacheProvider _cacheProvider;
        private readonly HttpClient _httpClient;

        public TcmbCurrencyService(ICacheProvider cacheProvider, HttpClient httpClient)
        {
            _cacheProvider = cacheProvider;
            _httpClient = httpClient;
        }

        public async Task<decimal> ConvertToTryAsync(decimal amount, string fromCurrency)
        {
            if (string.Equals(fromCurrency, "TRY", StringComparison.OrdinalIgnoreCase))
                return amount;

            decimal rate = await GetRateFromTcmbWithCacheAsync(fromCurrency.ToUpper());
            return amount * rate;
        }

        private async Task<decimal> GetRateFromTcmbWithCacheAsync(string currencyCode)
        {
            string cacheKey = $"ExchangeRate_{currencyCode}";
            
            // 1. Cache kontrolü
            var cachedRate = await _cacheProvider.GetAsync<decimal>(cacheKey);
            if (cachedRate > 0) return cachedRate;

            try
            {
                // 2. TCMB'den XML çekme
                var xmlString = await _httpClient.GetStringAsync("https://www.tcmb.gov.tr/kurlar/today.xml");
                var doc = XDocument.Parse(xmlString);

                // 3. XML Parse etme
                var currencyElement = doc.Descendants("Currency")
                    .FirstOrDefault(x => x.Attribute("CurrencyCode")?.Value == currencyCode);

                var forexBuyingStr = currencyElement?.Element("ForexBuying")?.Value;

                if (!string.IsNullOrEmpty(forexBuyingStr) && 
                    decimal.TryParse(forexBuyingStr, CultureInfo.InvariantCulture, out decimal rate))
                {
                    // 12 saatlik cache'e yaz
                    await _cacheProvider.SetAsync(cacheKey, rate, TimeSpan.FromHours(12));
                    return rate;
                }
            }
            catch (Exception)
            {
                // TCMB servisi çökerse veya internet giderse kurtarıcı (fallback) sabit kurlar
                return currencyCode switch
                {
                    "USD" => 33.50m,
                    "EUR" => 36.50m,
                    _ => 1.0m
                };
            }

            return 1.0m;
        }
    }
}

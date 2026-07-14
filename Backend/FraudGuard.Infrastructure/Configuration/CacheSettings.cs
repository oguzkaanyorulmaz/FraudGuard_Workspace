namespace FraudGuard.Infrastructure.Configuration
{
    public class CacheSettings
    {
        public bool PreferRedis { get; set; } = false;
        public string RedisConnectionString { get; set; }
        public int DefaultExpirationMinutes { get; set; } = 60;
    }
}
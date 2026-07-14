using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace FraudGuard.API.Middleware
{
    public class ApiKeyAuthMiddleware
    {
        private readonly RequestDelegate _next;
        private const string APIKEY_HEADER_NAME = "X-Api-Key";

        public ApiKeyAuthMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IConfiguration configuration)
        {
            if (!context.Request.Headers.TryGetValue(APIKEY_HEADER_NAME, out var extractedApiKey))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("API Key eksik!");
                return;
            }

            var validApiKey = configuration.GetValue<string>("SecuritySettings:ApiKey");

            if (validApiKey == null || !validApiKey.Equals(extractedApiKey))          
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync("Geçersiz API Key!");
                return;
            }

            await _next(context);
        }
    }
}
using FraudGuard.Domain.Common.Enums;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace FraudGuard.API.Middleware
{
    public class JwtAuthMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly string _secretKey;

        public JwtAuthMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            _next = next;
            _secretKey = configuration["JwtSettings:SecretKey"]
                ?? "FraudGuard-Super-Secret-JWT-Key-2026-MinLen32Chars!";
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Method == "OPTIONS")
            {
                await _next(context);
                return;
            }

            var path = context.Request.Path.Value?.ToLower() ?? "";
            if (path.Contains("/api/auth/login") ||
                path.Contains("/api/auth/register") ||
                path.Contains("/api/transactions/process") ||
                path.Contains("/api/transactions/transfer") ||
                path.Contains("/api/transactions/ping") ||
                path.Contains("/swagger") ||
                path.Contains("/fraudhub"))
            {
                await _next(context);
                return;
            }

            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Token bulunamadı.");
                return;
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_secretKey);

                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var username = jwtToken.Claims.First(c => c.Type == ClaimTypes.Name || c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name").Value;
                var roleValue = jwtToken.Claims.First(c => c.Type == ClaimTypes.Role || c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role").Value;

                context.Items["username"] = username;
                context.Items["role"] = (UserRoleEnum)int.Parse(roleValue);
            }
            catch
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Geçersiz veya süresi dolmuş token.");
                return;
            }

            await _next(context);
        }
    }
}

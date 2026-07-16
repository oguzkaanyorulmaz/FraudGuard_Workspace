using FraudGuard.Domain.Common.Enums;

namespace FraudGuard.Application.DTOs.Auth
{
    public class RegisterRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Mail { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public UserRoleEnum Role { get; set; }
    }
}

namespace FraudGuard.Application.DTOs.Auth
{
    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public int Role { get; set; }
        public string RoleName { get; set; } = string.Empty;
    }
}

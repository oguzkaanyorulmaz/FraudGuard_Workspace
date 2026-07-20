using FraudGuard.Domain.Common.Enums;
using FraudGuard.Domain.Interfaces.Abstractions;

namespace FraudGuard.Domain.Entities
{
    public class EUser
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Mail { get; set; } = string.Empty;
        public string PasswordUnderSHA256 { get; set; } = string.Empty;
        public UserRoleEnum Role { get; set; }

        public bool IsPasswordValid(string inputPassword, ICryptService cryptService)
        {
            return cryptService.VerifyPassword(inputPassword, this.PasswordUnderSHA256);
        }
    }
}

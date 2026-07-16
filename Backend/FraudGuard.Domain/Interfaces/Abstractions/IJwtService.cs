using FraudGuard.Domain.Common.Enums;

namespace FraudGuard.Domain.Interfaces.Abstractions
{
    public interface IJwtService
    {
        string GenerateToken(string username, UserRoleEnum role);
    }
}

namespace FraudGuard.Domain.Interfaces.Abstractions
{
    public interface ICryptService
    {
        string HashPassword(string password);
    }
}

using FraudGuard.Domain.Entities;

namespace FraudGuard.Domain.Interfaces.Repositories
{
    public interface IUserRepository
    {
        Task<EUser?> GetByUsernameAsync(string username);
        Task<bool> ExistsByUsernameAsync(string username);
        Task AddAsync(EUser user);
    }
}

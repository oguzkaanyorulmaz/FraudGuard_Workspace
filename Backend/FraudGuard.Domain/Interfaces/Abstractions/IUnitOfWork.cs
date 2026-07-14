using System.Threading.Tasks;

namespace FraudGuard.Domain.Interfaces.Abstractions
{
    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync();
    }
}
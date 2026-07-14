using FraudGuard.Domain.Interfaces.Abstractions;
using FraudGuard.Infrastructure.Persistence.Contexts;
using System.Threading.Tasks;

namespace FraudGuard.Infrastructure.Persistence
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly FraudGuardDbContext _context;

        public UnitOfWork(FraudGuardDbContext context)
        {
            _context = context;
        }

        public async Task<int> SaveChangesAsync()
        {
            // EF Core'un SaveChangesAsync metodu kendi içinde güvenli bir Transaction başlatır.
            // Eğer aradaki işlemlerden (Örn: Limit düşme veya Log yazma) biri bile hata verirse
            // işlemi Rollback (Geri al) yaparak veri tutarsızlığını önler.
            return await _context.SaveChangesAsync();
        }
    }
}
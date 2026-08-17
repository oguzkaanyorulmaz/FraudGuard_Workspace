using FraudGuard.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FraudGuard.Domain.Interfaces.Repositories
{
    public interface IFraudRuleRepository
    {
        Task<EFraudRule> GetByCodeAsync(string ruleCode);
        Task<List<EFraudRule>> GetAllActiveRulesAsync();

        /// <summary>Pasif kurallar dahil tüm katalog. Yönetim ekranı için.</summary>
        Task<List<EFraudRule>> GetAllAsync();

        /// <summary>Kural kodunun kullanımda olup olmadığını kontrol eder.</summary>
        Task<bool> ExistsByCodeAsync(string ruleCode);

        /// <summary>Yeni kural ekler. Kaydetmek için UnitOfWork.SaveChangesAsync çağrılmalıdır.</summary>
        Task AddAsync(EFraudRule rule);
    }
}

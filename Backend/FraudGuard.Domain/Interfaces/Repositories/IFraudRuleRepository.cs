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

        /// <summary>
        /// Tek kuralı takip edilir (tracked) olarak getirir.
        /// Güncelleme ve silme bu örnek üzerinden yapılır.
        /// </summary>
        Task<EFraudRule?> GetByIdAsync(int ruleId);

        /// <summary>Kural kodunun kullanımda olup olmadığını kontrol eder.</summary>
        Task<bool> ExistsByCodeAsync(string ruleCode);

        /// <summary>Yeni kural ekler. Kaydetmek için UnitOfWork.SaveChangesAsync çağrılmalıdır.</summary>
        Task AddAsync(EFraudRule rule);

        /// <summary>
        /// Kuralı kalıcı olarak siler. Kaydetmek için UnitOfWork.SaveChangesAsync çağrılmalıdır.
        /// Kurala bağlı fraud logu varsa veritabanı kısıtı silmeyi reddeder; çağıran taraf
        /// önce <see cref="IFraudLogRepository.AnyByRuleIdAsync"/> ile kontrol etmelidir.
        /// </summary>
        void Delete(EFraudRule rule);
    }
}

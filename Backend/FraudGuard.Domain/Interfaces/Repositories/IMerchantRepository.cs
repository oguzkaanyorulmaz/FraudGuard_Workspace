using System.Collections.Generic;
using System.Threading.Tasks;
using FraudGuard.Domain.Entities;

namespace FraudGuard.Domain.Interfaces.Repositories
{
    public interface IMerchantRepository
    {
        /// <summary>İşyeri kodu ile tek kayıt. Bulunamazsa null.</summary>
        Task<EMerchant?> GetByIdAsync(string merchantId);

        /// <summary>Aktif işyerleri. Simülatör ve yönetim ekranlarının seçim listesi.</summary>
        Task<List<EMerchant>> GetAllActiveAsync();
    }
}

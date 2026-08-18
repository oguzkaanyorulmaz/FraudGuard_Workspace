using System.Collections.Generic;
using System.Threading.Tasks;
using FraudGuard.Application.DTOs;
using FraudGuard.Application.DTOs.Merchant;

namespace FraudGuard.Application.Interfaces
{
    public interface IMerchantAppService
    {
        /// <summary>Aktif üye işyerleri. İşlem gönderirken seçilecek liste.</summary>
        Task<ResponseDTO<List<GetMerchantsResponse>>> GetActiveMerchantsAsync();
    }
}

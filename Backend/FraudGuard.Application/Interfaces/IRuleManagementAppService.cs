using System.Collections.Generic;
using System.Threading.Tasks;
using FraudGuard.Application.DTOs;
using FraudGuard.Application.DTOs.RuleManagement;

namespace FraudGuard.Application.Interfaces
{
    public interface IRuleManagementAppService
    {
        Task<ResponseDTO<List<GetActiveRulesResponse>>> GetActiveRulesAsync();

        /// <summary>Pasifler dahil tüm kural kataloğu.</summary>
        Task<ResponseDTO<List<GetActiveRulesResponse>>> GetAllRulesAsync();

        /// <summary>Bir ifadeyi kaydetmeden derleyip doğrular.</summary>
        Task<ResponseDTO<ValidateExpressionResponse>> ValidateExpressionAsync(ValidateExpressionRequest request);

        /// <summary>Yeni dinamik kural ekler. Geçersiz ifade kaydedilmez.</summary>
        Task<ResponseDTO<CreateFraudRuleResponse>> CreateRuleAsync(CreateFraudRuleRequest request);

        /// <summary>İfadelerde kullanılabilecek alanların listesi.</summary>
        ResponseDTO<List<RuleFieldDto>> GetAvailableFields();
    }
}

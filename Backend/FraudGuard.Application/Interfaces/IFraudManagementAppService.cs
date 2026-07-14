using System.Collections.Generic;
using System.Threading.Tasks;
using FraudGuard.Application.DTOs;
using FraudGuard.Application.DTOs.FraudManagement;

namespace FraudGuard.Application.Interfaces
{
    public interface IFraudManagementAppService
    {
        Task<ResponseDTO<List<GetUnresolvedLogsResponse>>> GetUnresolvedLogsAsync();
        Task<ResponseDTO<bool>> ResolveLogAsync(ResolveFraudLogRequest request);
        Task<ResponseDTO<GetFraudLogDetailResponse>> GetLogDetailAsync(int logId);
        Task<ResponseDTO<List<GetUnresolvedLogsResponse>>> GetResolvedLogsAsync();
    }
}
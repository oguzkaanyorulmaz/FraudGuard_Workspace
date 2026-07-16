using System.Collections.Generic;
using System.Threading.Tasks;
using FraudGuard.Application.DTOs;
using FraudGuard.Application.DTOs.FraudManagement;
using FraudGuard.Domain.Common.Enums;

namespace FraudGuard.Application.Interfaces
{
    public interface IFraudManagementAppService
    {
        Task<ResponseDTO<List<GetUnresolvedLogsResponse>>> GetUnresolvedLogsAsync();
        Task<ResponseDTO<bool>> ResolveLogAsync(ResolveFraudLogRequest request);
        Task<ResponseDTO<GetFraudLogDetailResponse>> GetLogDetailAsync(int logId, UserRoleEnum callerRole);
        Task<ResponseDTO<List<GetUnresolvedLogsResponse>>> GetResolvedLogsAsync();
    }
}
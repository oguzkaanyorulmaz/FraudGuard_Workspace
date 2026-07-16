using FraudGuard.Application.DTOs;
using FraudGuard.Application.DTOs.Auth;

namespace FraudGuard.Application.Interfaces
{
    public interface IAuthAppService
    {
        Task<ResponseDTO<LoginResponse>> LoginAsync(LoginRequest request);
        Task<ResponseDTO<bool>> RegisterAsync(RegisterRequest request);
    }
}

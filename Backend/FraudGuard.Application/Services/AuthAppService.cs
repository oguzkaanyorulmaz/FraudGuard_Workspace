using FraudGuard.Application.DTOs;
using FraudGuard.Application.DTOs.Auth;
using FraudGuard.Application.Interfaces;
using FraudGuard.Domain.Interfaces.Abstractions;
using FraudGuard.Domain.Interfaces.Repositories;

namespace FraudGuard.Application.Services
{
    public class AuthAppService : IAuthAppService
    {
        private readonly IUserRepository _userRepository;
        private readonly ICryptService _cryptService;
        private readonly IJwtService _jwtService;
        private readonly IUnitOfWork _unitOfWork;

        public AuthAppService(IUserRepository userRepository, ICryptService cryptService, IJwtService jwtService, IUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _cryptService = cryptService;
            _jwtService = jwtService;
            _unitOfWork = unitOfWork;
        }

        public async Task<ResponseDTO<LoginResponse>> LoginAsync(LoginRequest request)
        {
            var user = await _userRepository.GetByUsernameAsync(request.Username);
            if (user == null)
            {
                return ResponseDTO<LoginResponse>.Fail("Kullanıcı bulunamadı.");
            }

            if (!user.IsPasswordValid(request.Password, _cryptService))
            {
                return ResponseDTO<LoginResponse>.Fail("Hatalı şifre.");
            }

            string token = _jwtService.GenerateToken(user.Username, user.Role);

            var response = new LoginResponse
            {
                Token = token,
                Username = user.Username,
                Role = (int)user.Role,
                RoleName = user.Role.ToString()
            };

            return ResponseDTO<LoginResponse>.Success(response, "Giriş başarılı.");
        }

        public async Task<ResponseDTO<bool>> RegisterAsync(RegisterRequest request)
        {
            if (await _userRepository.ExistsByUsernameAsync(request.Username))
            {
                return ResponseDTO<bool>.Fail("Bu kullanıcı adı zaten alınmış.");
            }

            var newUser = new Domain.Entities.EUser
            {
                Username = request.Username,
                Mail = request.Mail,
                PasswordUnderSHA256 = _cryptService.HashPassword(request.Password),
                Role = request.Role
            };

            await _userRepository.AddAsync(newUser);
            await _unitOfWork.SaveChangesAsync();

            return ResponseDTO<bool>.Success(true, "Kayıt işlemi başarılı.");
        }
    }
}

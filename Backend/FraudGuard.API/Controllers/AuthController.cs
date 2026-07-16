using Microsoft.AspNetCore.Mvc;
using FraudGuard.Application.Interfaces;
using FraudGuard.Application.DTOs.Auth;

namespace FraudGuard.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthAppService _authAppService;

        public AuthController(IAuthAppService authAppService)
        {
            _authAppService = authAppService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var response = await _authAppService.LoginAsync(request);

            if (response.IsSuccess)
                return Ok(response);

            return BadRequest(response);
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var response = await _authAppService.RegisterAsync(request);

            if (response.IsSuccess)
                return Ok(response);

            return BadRequest(response);
        }
    }
}

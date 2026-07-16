using Microsoft.AspNetCore.Mvc;
using FraudGuard.Application.Interfaces;
using FraudGuard.Application.DTOs.FraudManagement;
using System.Threading.Tasks;
using FraudGuard.API.Middleware;
using FraudGuard.Domain.Common.Enums;


namespace FraudGuard.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FraudManagementController : ControllerBase
    {
        private readonly IFraudManagementAppService _fraudManagementAppService;

        public FraudManagementController(IFraudManagementAppService fraudManagementAppService)
        {
            _fraudManagementAppService = fraudManagementAppService;
        }

        [HttpGet("unresolved-logs")]
        public async Task<IActionResult> GetUnresolvedLogs()
        {
            var response = await _fraudManagementAppService.GetUnresolvedLogsAsync();
            
            if (response.IsSuccess)
                return Ok(response);
                
            return BadRequest(response);
        }

        [HttpGet("resolved-logs")]
        public async Task<IActionResult> GetResolvedLogs()
        {
            var result = await _fraudManagementAppService.GetResolvedLogsAsync();
            return Ok(result);
        }

        [RoleRequired(UserRoleEnum.Admin, UserRoleEnum.DecisionMaker)]
[HttpPost("resolve-log")]
public async Task<IActionResult> ResolveLog([FromBody] ResolveFraudLogRequest request)
        {
            var response = await _fraudManagementAppService.ResolveLogAsync(request);
            
            if (response.IsSuccess)
                return Ok(response);
                
            return BadRequest(response);
        }

                [HttpGet("log-detail/{logId}")]
        public async Task<IActionResult> GetLogDetail(int logId)
        {
            var role = HttpContext.Items["role"] as UserRoleEnum? ?? UserRoleEnum.Analyst;
            var response = await _fraudManagementAppService.GetLogDetailAsync(logId, role);

            if (response.IsSuccess)
                return Ok(response);

            return BadRequest(response);
        }


        
    }
}
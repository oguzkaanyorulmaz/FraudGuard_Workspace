using Microsoft.AspNetCore.Mvc;
using FraudGuard.Application.Interfaces;
using System.Threading.Tasks;

namespace FraudGuard.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RuleManagementController : ControllerBase
    {
        private readonly IRuleManagementAppService _ruleManagementAppService;

        public RuleManagementController(IRuleManagementAppService ruleManagementAppService)
        {
            _ruleManagementAppService = ruleManagementAppService;
        }

        [HttpGet("active-rules")]
        public async Task<IActionResult> GetActiveRules()
        {
            var response = await _ruleManagementAppService.GetActiveRulesAsync();
            
            if (response.IsSuccess)
                return Ok(response);
                
            return BadRequest(response);
        }
    }
}
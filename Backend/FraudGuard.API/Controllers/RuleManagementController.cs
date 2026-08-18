using Microsoft.AspNetCore.Mvc;
using FraudGuard.Application.Interfaces;
using FraudGuard.Application.DTOs.RuleManagement;
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
            return response.IsSuccess ? Ok(response) : BadRequest(response);
        }

        /// <summary>Pasifler dahil tüm kural kataloğu.</summary>
        [HttpGet("all-rules")]
        public async Task<IActionResult> GetAllRules()
        {
            var response = await _ruleManagementAppService.GetAllRulesAsync();
            return response.IsSuccess ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// İfadelerde kullanılabilecek alanların listesi.
        /// <c>isPopulated=false</c> olan alanlar çalışma anında dolmaz; onları kullanan kural tetiklenmez.
        /// </summary>
        [HttpGet("available-fields")]
        public IActionResult GetAvailableFields()
        {
            var response = _ruleManagementAppService.GetAvailableFields();
            return response.IsSuccess ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Bir ifadeyi kaydetmeden derleyip doğrular. Kural yazarken ilk uğranacak uç.
        /// </summary>
        [HttpPost("validate-expression")]
        public async Task<IActionResult> ValidateExpression([FromBody] ValidateExpressionRequest request)
        {
            var response = await _ruleManagementAppService.ValidateExpressionAsync(request);
            return response.IsSuccess ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Yeni dinamik kural ekler. İfade derlenemezse kural kaydedilmez.
        /// Kaydedilen kural, motor kural listesini önbelleğe almadığı için
        /// bir sonraki işlemden itibaren devrededir; yeniden başlatma gerekmez.
        /// </summary>
        [HttpPost("rules")]
        public async Task<IActionResult> CreateRule([FromBody] CreateFraudRuleRequest request)
        {
            var response = await _ruleManagementAppService.CreateRuleAsync(request);
            return response.IsSuccess ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Kuralı kalıcı olarak siler. Kural geçmiş fraud alarmlarına bağlıysa silinmez;
        /// bu durumda yalnızca pasife alınabilir.
        /// </summary>
        [HttpDelete("rules/{ruleId:int}")]
        public async Task<IActionResult> DeleteRule(int ruleId)
        {
            var response = await _ruleManagementAppService.DeleteRuleAsync(ruleId);
            return response.IsSuccess ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Kuralı silmeden devre dışı bırakır veya geri açar.
        /// Silinemeyen kuralları etkisiz hale getirmenin yolu budur.
        /// </summary>
        [HttpPatch("rules/{ruleId:int}/status")]
        public async Task<IActionResult> SetRuleStatus(int ruleId, [FromBody] SetRuleStatusRequest request)
        {
            var response = await _ruleManagementAppService.SetRuleStatusAsync(ruleId, request);
            return response.IsSuccess ? Ok(response) : BadRequest(response);
        }
    }
}

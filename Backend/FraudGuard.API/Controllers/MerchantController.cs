using System.Threading.Tasks;
using FraudGuard.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FraudGuard.API.Controllers
{
    /// <summary>
    /// Üye işyeri kataloğu. Simülatör ve yönetim ekranlarının işyeri seçim listesini besler.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class MerchantController : ControllerBase
    {
        private readonly IMerchantAppService _merchantAppService;

        public MerchantController(IMerchantAppService merchantAppService)
        {
            _merchantAppService = merchantAppService;
        }

        [HttpGet]
        public async Task<IActionResult> GetActiveMerchants()
        {
            var response = await _merchantAppService.GetActiveMerchantsAsync();
            return response.IsSuccess ? Ok(response) : BadRequest(response);
        }
    }
}

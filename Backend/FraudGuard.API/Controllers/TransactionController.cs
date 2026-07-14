using Microsoft.AspNetCore.Mvc;
using FraudGuard.Application.Interfaces;
using FraudGuard.Application.DTOs.TransactionProcessing;
using System.Threading.Tasks;

namespace FraudGuard.API.Controllers
{
    [ApiController]
    [Route("api/transactions")] 
    public class TransactionController : ControllerBase
    {
        private readonly ITransactionAppService _transactionAppService;

        public TransactionController(ITransactionAppService transactionAppService)
        {
            _transactionAppService = transactionAppService;
        }

        [HttpPost("process")]
        public async Task<IActionResult> ProcessTransaction([FromBody] ProcessTransactionRequest request)
        {
            var response = await _transactionAppService.ProcessAsync(request);
            
            if (response.IsSuccess)
                return Ok(response);
                
            return BadRequest(response);
        }
    }
}
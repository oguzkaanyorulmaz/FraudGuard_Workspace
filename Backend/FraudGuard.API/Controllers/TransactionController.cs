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
            try 
            {
                var response = await _transactionAppService.ProcessAsync(request);
                return response.IsSuccess ? Ok(response) : BadRequest(response);
            }
            catch (Exception ex)
            {
                // Hatayı tam burada yakalayıp konsola dökeceğiz
                Console.WriteLine($"🚨 İŞLEM HATASI: {ex.Message}");
                if (ex.InnerException != null) 
                    Console.WriteLine($"🔍 INNER EXCEPTION: {ex.InnerException.Message}");
                    
                return StatusCode(500, new { message = "Hata detayına terminalden bak!", error = ex.Message });
            }
        }
    }
}
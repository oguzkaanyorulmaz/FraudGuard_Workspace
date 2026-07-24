using Microsoft.AspNetCore.Mvc;
using FraudGuard.Application.Interfaces;
using FraudGuard.Application.DTOs.TransactionProcessing;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using FraudGuard.API.Hubs;

namespace FraudGuard.API.Controllers
{
    [ApiController]
    [Route("api/transactions")] 
    public class TransactionController : ControllerBase
    {
        private readonly ITransactionAppService _transactionAppService;
        private readonly IHubContext<FraudHub> _hubContext;

        public TransactionController(ITransactionAppService transactionAppService, IHubContext<FraudHub> hubContext)
        {
            _transactionAppService = transactionAppService;
            _hubContext = hubContext;
        }

                [HttpPost("process")]
        public async Task<IActionResult> ProcessTransaction([FromBody] ProcessTransactionRequest request)
        {
            try 
            {
                var response = await _transactionAppService.ProcessAsync(request);
                if (response.IsSuccess)
                {
                    await _hubContext.Clients.All.SendAsync("RefreshLogs");
                }
                return response.IsSuccess ? Ok(response) : BadRequest(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🚨 İŞLEM HATASI: {ex.Message}");
                if (ex.InnerException != null) 
                    Console.WriteLine($"🔍 INNER EXCEPTION: {ex.InnerException.Message}");
                    
                return StatusCode(500, new { message = "Hata detayına terminalden bak!", error = ex.Message });
            }
        }

                [HttpPost("transfer")]
        public async Task<IActionResult> ProcessTransfer([FromBody] ProcessTransferRequest request)
        {
            try
            {
                var response = await _transactionAppService.ProcessTransferAsync(request);
                if (response.IsSuccess)
                {
                    await _hubContext.Clients.All.SendAsync("RefreshLogs");
                }
                return response.IsSuccess ? Ok(response) : BadRequest(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🚨 TRANSFER HATASI: {ex.Message}");
                return StatusCode(500, new { message = "Hata detayına terminalden bak!", error = ex.Message });
            }
        }

                [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok("pong");
        }


    }
}
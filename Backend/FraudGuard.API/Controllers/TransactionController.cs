using Microsoft.AspNetCore.Mvc;
using FraudGuard.Application.Interfaces;
using FraudGuard.Application.DTOs;
using FraudGuard.Application.DTOs.TransactionProcessing;
using System;
using System.Linq;
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
        private readonly IHubContext<FraudHub, IFraudHubClient> _hubContext;

        public TransactionController(
            ITransactionAppService transactionAppService,
            IHubContext<FraudHub, IFraudHubClient> hubContext)
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
                    await BroadcastAsync(response);

                return response.IsSuccess ? Ok(response) : BadRequest(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"İŞLEM HATASI: {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($"INNER EXCEPTION: {ex.InnerException.Message}");

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
                    await BroadcastAsync(response);

                return response.IsSuccess ? Ok(response) : BadRequest(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"TRANSFER HATASI: {ex.Message}");
                return StatusCode(500, new { message = "Hata detayına terminalden bak!", error = ex.Message });
            }
        }

        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok("pong");
        }

        /// <summary>
        /// Karar sonucunu panele yayınlar.
        /// <c>RefreshLogs</c> mevcut istemcinin listeyi tazelemesi için korunur;
        /// <c>FraudDecision</c> skor kırılımını taşıyan yeni kanaldır.
        /// </summary>
        private async Task BroadcastAsync(ResponseDTO<ProcessTransactionResponse> response)
        {
            await _hubContext.Clients.All.RefreshLogs();

            var payload = response.Data;
            if (payload is null)
                return;

            await _hubContext.Clients.All.FraudDecision(new FraudDecisionNotification
            {
                TransactionId = payload.TransactionId,
                RRN = payload.RRN,
                Status = payload.Status,
                Decision = payload.Decision,
                RiskScore = payload.RiskScore,
                CardRiskScore = payload.CardRiskScore,
                MerchantRiskScore = payload.MerchantRiskScore,
                RawRuleScore = payload.RawRuleScore,
                TotalBonusScore = payload.TotalBonusScore,
                TotalTrustDiscount = payload.TotalTrustDiscount,
                RequiresAdditionalVerification = payload.RequiresAdditionalVerification,
                TriggeredRules = payload.TriggeredRules.Cast<object>().ToArray(),
                AppliedCombinations = payload.AppliedCombinations.Cast<object>().ToArray(),
                TrustFactors = payload.TrustFactors.ToArray(),
                OccurredAt = DateTime.Now.ToString("o")
            });
        }
    }
}

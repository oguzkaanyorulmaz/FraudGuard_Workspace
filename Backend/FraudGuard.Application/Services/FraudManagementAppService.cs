using AutoMapper;
using FraudGuard.Application.DTOs;
using FraudGuard.Application.DTOs.FraudManagement;
using FraudGuard.Application.Interfaces;
using FraudGuard.Domain.Interfaces.DomainServices;
using FraudGuard.Domain.Interfaces.Repositories; 
using System.Linq; 
using Microsoft.EntityFrameworkCore; 
using System.Collections.Generic;
using System.Threading.Tasks;
using FraudGuard.Domain.Common.Enums;
using FraudGuard.Application.Helpers;


namespace FraudGuard.Application.Services
{
    public class FraudManagementAppService : IFraudManagementAppService
    {
        private readonly IAdminOperationService _adminOperationService;
        private readonly IMapper _mapper;
        private readonly IFraudLogRepository _fraudLogRepository;
        private readonly ITransactionRepository _transactionRepository;

        public FraudManagementAppService(IAdminOperationService adminOperationService, IMapper mapper, IFraudLogRepository fraudLogRepository, ITransactionRepository transactionRepository)
        {
            _adminOperationService = adminOperationService;
            _mapper = mapper;
            _fraudLogRepository = fraudLogRepository;
            _transactionRepository = transactionRepository;
        }

        public async Task<ResponseDTO<List<GetUnresolvedLogsResponse>>> GetUnresolvedLogsAsync()
        {
            var logs = await _adminOperationService.GetUnresolvedLogsAsync();
            
            var responseList = _mapper.Map<List<GetUnresolvedLogsResponse>>(logs);

            for (int i = 0; i < responseList.Count; i++)
            {
                var item = responseList[i];
                var originalLog = logs[i];

                item.SuspicionReason = originalLog.Transaction?.FraudReason ?? "Sistem tarafından şüpheli bulundu.";
                item.AdminAction = originalLog.AdminAction;


                item.RuleName = originalLog.FraudRule?.RuleName ?? "Genel Şüpheli İşlem";

                var tx = originalLog.Transaction;
                var card = tx?.CreditCard;

                if (tx != null && card != null && originalLog.FraudRule != null)
                {
                    item.RiskScore = CalculateRiskScore(
                        originalLog.FraudRule.RuleCode,
                        tx.Amount,
                        card.CardLimit,
                        card.AvailableLimit
                    );
                }
                else
                {
                    item.RiskScore = 75;
                }
            }

            return ResponseDTO<List<GetUnresolvedLogsResponse>>.Success(responseList);
        }

        public async Task<ResponseDTO<bool>> ResolveLogAsync(ResolveFraudLogRequest request)
        {
            var result = await _adminOperationService.ResolveFraudLogAsync(
                request.LogId, 
                request.AdminAction, 
                request.AdminNote, 
                request.BlockReasonId,
                request.ResolvedByAdmin
            );
            
            if (result)
                return ResponseDTO<bool>.Success(true, "Log başarıyla çözümlendi.");
            
            return ResponseDTO<bool>.Fail("Log çözümlenirken bir hata oluştu veya log bulunamadı.");
        }

        public async Task<ResponseDTO<GetFraudLogDetailResponse>> GetLogDetailAsync(int logId, UserRoleEnum callerRole)
        {
            var logEntity = await _fraudLogRepository.GetLogWithDetailsAsync(logId);
            if (logEntity == null)
            {
                return ResponseDTO<GetFraudLogDetailResponse>.Fail("Log detayları bulunamadı.");
            }
            var recentTxList = await _transactionRepository.GetLast10TransactionsForCardAsync(logEntity.Transaction.CardId, logEntity.TransactionId);
            var detail = new GetFraudLogDetailResponse
            {
                LogId = logEntity.LogId, 
                TransactionId = logEntity.TransactionId,
                Amount = logEntity.Transaction.Amount,
                Currency = logEntity.Transaction.Currency,
                TransactionDate = logEntity.Transaction.TransactionDate,
                Location = logEntity.Transaction.Location,
                Country = logEntity.Transaction.Country,
                TransactionTypeName = logEntity.Transaction.TransactionType?.Description ?? "Bilinmeyen",
                
                MaskedCardNumber = logEntity.Transaction.CreditCard.CardNumber, 
                CardLimit = logEntity.Transaction.CreditCard.CardLimit,
                AvailableLimit = logEntity.Transaction.CreditCard.AvailableLimit,
                IsCardBlocked = logEntity.Transaction.CreditCard.IsBlocked,
                AdminNote = logEntity.AdminNote,
                ResolvedByAdmin = logEntity.ResolvedByAdmin,
                
                CustomerFullName = $"{logEntity.Transaction.CreditCard.Customer.FirstName} {logEntity.Transaction.CreditCard.Customer.LastName}",
                IdentityNumber = logEntity.Transaction.CreditCard.Customer.IdentityNumber,
                PhoneNumber = logEntity.Transaction.CreditCard.Customer.PhoneNumber, 
                
                RuleName = logEntity.FraudRule?.RuleName ?? "Genel Şüpheli İşlem",
                SuspicionReason = logEntity.Transaction.FraudReason ?? "Sistem tarafından şüpheli bulundu.",
                FraudReason = logEntity.Transaction.FraudReason,
                
                RecentTransactions = recentTxList.Select(t => new CardRecentTransactionDto
                {
                    Amount = t.Amount,
                    Currency = t.Currency,
                    Location = t.Location,
                    Country = t.Country,
                    TransactionTypeName = t.TransactionType?.Description ?? "Bilinmeyen",
                    TransactionDate = t.TransactionDate,
                    MerchantCategory = t.MerchantCategory,
                    Status = t.Status,
                    
                    FraudSuspicionReason = (t.Status == "Approved" && t.FraudLog != null) ? (t.FraudLog.FraudRule?.RuleName ?? t.FraudReason) : null,
                    AdminNote = (t.Status == "Approved" && t.FraudLog != null) ? t.FraudLog.AdminNote : null,
                    
                    ResolvedByAdmin = (t.Status == "Approved" && t.FraudLog != null) ? t.FraudLog.ResolvedByAdmin : null,
                    DeclineReason = t.DeclineReason
                }).ToList()
            };

            // Rol bazlı veri maskeleme
            if (callerRole != UserRoleEnum.Admin)
            {
                detail.MaskedCardNumber = detail.MaskedCardNumber.MaskCardNumber();
                detail.IdentityNumber = detail.IdentityNumber.MaskIdentityNumber();
                detail.PhoneNumber = detail.PhoneNumber.MaskPhoneNumber();
            }

            return ResponseDTO<GetFraudLogDetailResponse>.Success(detail);
        }

        public async Task<ResponseDTO<List<GetUnresolvedLogsResponse>>> GetResolvedLogsAsync()
        {
            var logs = await _fraudLogRepository.GetResolvedLogsAsync();
            var responseList = _mapper.Map<List<GetUnresolvedLogsResponse>>(logs);

            for (int i = 0; i < responseList.Count; i++)
            {
                var item = responseList[i];
                var originalLog = logs[i]; 

                item.SuspicionReason = originalLog.Transaction?.FraudReason ?? "Sistem tarafından şüpheli bulundu.";
                item.AdminAction = originalLog.AdminAction;

                item.RuleName = originalLog.FraudRule?.RuleName ?? "Genel Şüpheli İşlem";

                var tx = originalLog.Transaction;
                var card = tx?.CreditCard;

                if (tx != null && card != null && originalLog.FraudRule != null)
                {
                    item.RiskScore = CalculateRiskScore(
                        originalLog.FraudRule.RuleCode,
                        tx.Amount,
                        card.CardLimit,
                        card.AvailableLimit
                    );
                }
                else
                {
                    item.RiskScore = 75;
                }
            }

            return ResponseDTO<List<GetUnresolvedLogsResponse>>.Success(responseList);
        }

        private int CalculateRiskScore(string ruleCode, decimal txAmount, decimal cardLimit, decimal availableLimit)
        {
            int ruleWeight = ruleCode switch
            {
                "IMPOSSIBLE_TRAVEL" => 95,
                "BRUTE_FORCE" => 90,
                "MAX_OUT" => 85,
                "ANOMALOUS_TIME" => 80,
                "CARD_TESTING" => 75,
                "CROSS_BORDER" => 70,
                "HIGH_RISK_MCC" => 65,
                "CONSECUTIVE_REFUNDS" => 60,
                "CURRENCY_MISMATCH" => 55,
                "VELOCITY" => 50,
                _ => 60
            };

            decimal limitEtki = 0;
            if (cardLimit > 0)
            {
                decimal spentLimit = System.Math.Max(0, cardLimit - availableLimit);
                decimal txRatio = (txAmount / cardLimit) * 100;
                decimal spentRatio = (spentLimit / cardLimit) * 100;
                limitEtki = (txRatio * 0.5m) + (spentRatio * 0.5m);
                if (limitEtki > 100) limitEtki = 100;
            }

            decimal hacimSkoru = System.Math.Min((txAmount / 50000m) * 100m, 100m);
            
            decimal factor = ((limitEtki * 0.5m) + (hacimSkoru * 0.5m)) / 100m;
            
            decimal totalScore = ruleWeight + (100m - ruleWeight) * factor;

            int finalScore = (int)System.Math.Round(totalScore);
            return System.Math.Clamp(finalScore, 1, 100);
        }
    }
}
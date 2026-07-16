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


                switch (originalLog.RuleId) 
                {
                    case 1:
                        item.RuleName = "Para Birimi Anormalliği";
                        item.RiskScore = 65;
                        break;
                    case 2:
                        item.RuleName = "İmkansız Seyahat / Hız";
                        item.RiskScore = 98;
                        break;
                    case 3:
                        item.RuleName = "Yüksek Tutar / Limit Boşaltma";
                        item.RiskScore = 85;
                        break;
                    default:
                        item.RuleName = originalLog.FraudRule?.RuleName ?? "Genel Şüpheli İşlem";
                        item.RiskScore = 75; 
                        break;
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

        public async Task<ResponseDTO<GetFraudLogDetailResponse>> GetLogDetailAsync(int logId)
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

                switch (originalLog.RuleId) 
                {
                    case 1:
                        item.RuleName = "Para Birimi Anormalliği";
                        item.RiskScore = 65;
                        break;
                    case 2:
                        item.RuleName = "İmkansız Seyahat / Hız";
                        item.RiskScore = 98;
                        break;
                    case 3:
                        item.RuleName = "Yüksek Tutar / Limit Boşaltma";
                        item.RiskScore = 85;
                        break;
                    default:
                        item.RuleName = originalLog.FraudRule?.RuleName ?? "Genel Şüpheli İşlem";
                        item.RiskScore = 75; 
                        break;
                }
                
            }

            return ResponseDTO<List<GetUnresolvedLogsResponse>>.Success(responseList);
        }
    }
}
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

        public FraudManagementAppService(IAdminOperationService adminOperationService, IMapper mapper, IFraudLogRepository fraudLogRepository)
        {
            _adminOperationService = adminOperationService;
            _mapper = mapper;
            _fraudLogRepository = fraudLogRepository;
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
                request.BlockReasonId
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

            var detail = new GetFraudLogDetailResponse
            
            {
                LogId = logEntity.LogId, 
                TransactionId = logEntity.TransactionId,
                
                Amount = logEntity.Transaction.Amount,
                Currency = logEntity.Transaction.Currency,
                TransactionDate = logEntity.Transaction.TransactionDate,
                Location = logEntity.Transaction.Location,
                Country = logEntity.Transaction.Country,
                
                MaskedCardNumber = logEntity.Transaction.CreditCard.CardNumber, 
                CardLimit = logEntity.Transaction.CreditCard.CardLimit,
                IsCardBlocked = logEntity.Transaction.CreditCard.IsBlocked,
                
                CustomerFullName = $"{logEntity.Transaction.CreditCard.Customer.FirstName} {logEntity.Transaction.CreditCard.Customer.LastName}",
                IdentityNumber = logEntity.Transaction.CreditCard.Customer.IdentityNumber,
                
                RuleName = "Para Birimi Anormalliği",
                SuspicionReason = "Müşteri geçmişinde işlem kaydı bulunmuyor.",
                FraudReason = logEntity.Transaction.FraudReason
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
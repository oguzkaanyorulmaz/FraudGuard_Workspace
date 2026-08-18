using AutoMapper;
using FraudGuard.Application.DTOs;
using FraudGuard.Application.DTOs.FraudManagement;
using FraudGuard.Application.Interfaces;
using FraudGuard.Domain.Interfaces.Repositories; 
using System.Collections.Generic;
using System.Threading.Tasks;
using FraudGuard.Domain.Common.Enums;
using FraudGuard.Application.Helpers;
using FraudGuard.Domain.Entities;
using FraudGuard.Domain.Interfaces.Entities;


namespace FraudGuard.Application.Services
{
    public class FraudManagementAppService : IFraudManagementAppService
    {
        private readonly IAdminOperationService _adminOperationService;
        private readonly IMapper _mapper;
        private readonly IFraudLogRepository _fraudLogRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly IDebitCardRepository _debitCardRepository;

        public FraudManagementAppService(IAdminOperationService adminOperationService, IMapper mapper, IFraudLogRepository fraudLogRepository, ITransactionRepository transactionRepository, IDebitCardRepository debitCardRepository)
        {
            _adminOperationService = adminOperationService;
            _mapper = mapper;
            _fraudLogRepository = fraudLogRepository;
            _transactionRepository = transactionRepository;
            _debitCardRepository = debitCardRepository;
        }

        public async Task<ResponseDTO<List<GetUnresolvedLogsResponse>>> GetUnresolvedLogsAsync(UserRoleEnum callerRole)
        {
            var logs = await _adminOperationService.GetUnresolvedLogsAsync();
            var responseList = _mapper.Map<List<GetUnresolvedLogsResponse>>(logs);
            var pendingLogsResponse = new List<GetUnresolvedLogsResponse>();

            for (int i = 0; i < responseList.Count; i++)
            {
                var item = responseList[i];
                var originalLog = logs[i];

                item.SuspicionReason = originalLog.Transaction?.FraudReason ?? "Sistem tarafından şüpheli bulundu.";
                item.AdminAction = originalLog.AdminAction;
                item.RuleName = originalLog.FraudRule?.RuleName ?? "Genel Şüpheli İşlem";

                var tx = originalLog.Transaction;
                var cc = originalLog.CreditCardTransaction?.CreditCard;
                var dc = originalLog.DebitCardTransaction?.DebitCard;

                // Skor işlem anında motor tarafından hesaplanıp kaydedilmiştir veya FraudReason özetinden okunur.
                int resolvedScore = tx?.RiskScore ?? 0;
                if (!string.IsNullOrEmpty(tx?.FraudReason))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(tx.FraudReason, @"Skor\s+(\d+)");
                    if (match.Success && int.TryParse(match.Groups[1].Value, out int parsedScore))
                    {
                        resolvedScore = Math.Min(100, parsedScore);
                    }
                }
                item.RiskScore = resolvedScore;
                item.RiskDecision = RiskDecisionNames.ToWireFormat(tx?.RiskDecision ?? RiskDecisionEnum.Normal);

                if (callerRole == UserRoleEnum.Admin)
                {
                    item.MaskedCardNumber = cc?.CardNumber ?? dc?.CardNumber ?? originalLog.Transaction?.SenderIBAN ?? "Bilinmiyor";
                }

                // 🛡️ OTOMATİK HATA YAKALAMA & KENDİNİ ONARMA (Self-Healing Auto-Catcher)
                // 0 - 39 (Normal / Sistem Onayı): Bu işlemler şüpheli değildir; analist ekranında veya Fraud loglarında yer almaz.
                if (resolvedScore < 40)
                {
                    await _fraudLogRepository.DeleteAsync(originalLog.LogId);
                }
                else if (resolvedScore >= 90)
                {
                    // 90 - 100 (Ret / Bloke): Sistem tarafından otomatik bloke edilmiş olarak çözümlenir
                    await _adminOperationService.ResolveFraudLogAsync(
                        originalLog.LogId,
                        "BLOCKED",
                        "Sistem: Risk skoru 90-100 (Ret/Bloke) aralığında olduğu için otomatik bloke edildi.",
                        1,
                        "Sistem (Otomatik Bloke)"
                    );
                }
                else
                {
                    // Yalnızca 40 - 89 Puan (İzle & Ek Doğrulama) arası gerçekten analist onayı bekleyen işlemler listelenir
                    pendingLogsResponse.Add(item);
                }
            }

            return ResponseDTO<List<GetUnresolvedLogsResponse>>.Success(pendingLogsResponse);
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
            bool isCredit = logEntity.CreditCardTransactionId.HasValue;
            var targetCardId = logEntity.Transaction?.CreditCardId ?? logEntity.Transaction?.DebitCardId ?? 0;
            var activeTxDate = logEntity.Transaction?.TransactionDate ?? DateTime.UtcNow;
            
            var recentTxList = await _transactionRepository.GetLast10TransactionsForCardAsync(
                targetCardId, 
                isCredit, 
                activeTxDate);
            var recentSuspiciousTxList = await _transactionRepository.GetLast10SuspiciousTransactionsForCardAsync(
                targetCardId,
                isCredit,
                activeTxDate);
            var creditCard = logEntity.CreditCardTransaction?.CreditCard;
            var debitCard = logEntity.DebitCardTransaction?.DebitCard;
            var customer = creditCard?.Customer ?? debitCard?.Customer;
            var isCardSuspicious = await _transactionRepository.HasAnySuspiciousTransactionAsync(targetCardId, isCredit);

            // Transfer geçmişi: Hesabın IBAN'ı üzerinden gönderilen ve alınan EFT/Havale işlemleri
            var sentTransferList = new List<ETransferTransaction>();
            var receivedTransferList = new List<ETransferTransaction>();
            string? accountIBAN = null;
            
            if (logEntity.TransferTransactionId.HasValue)
            {
                // Transfer tipi log: SenderIBAN'dan hesap IBAN'ını al
                accountIBAN = logEntity.Transaction?.SenderIBAN;
                if (!string.IsNullOrEmpty(accountIBAN))
                {
                    var senderDebit = await _debitCardRepository.GetByIBANAsync(accountIBAN);
                    if (senderDebit != null)
                    {
                        customer = customer ?? senderDebit.Customer;
                    }
                }
            }
            else if (debitCard != null)
            {
                // Banka kartı tipi log: DebitCard'ın IBAN'ını kullan
                accountIBAN = debitCard.IBAN;
            }
            
            if (!string.IsNullOrEmpty(accountIBAN))
            {
                sentTransferList = await _transactionRepository.GetLast10SentTransfersForIBANAsync(accountIBAN, activeTxDate);
                receivedTransferList = await _transactionRepository.GetLast10ReceivedTransfersForIBANAsync(accountIBAN, activeTxDate);
            }

            // Transfer DTO mapper helper
            CardRecentTransactionDto MapTransferToDto(ETransferTransaction t) => new CardRecentTransactionDto
            {
                Amount = t.Amount,
                Currency = t.Currency,
                Location = t.Location,
                Country = t.Country,
                TransactionTypeName = "Transfer İşlemi",
                TransactionDate = t.TransactionDate,
                MerchantCategory = t.MerchantCategory,
                Status = t.Status,
                FraudSuspicionReason = (t.Status == "Approved" && t.FraudLog != null) ? (t.FraudLog.FraudRule?.RuleName ?? t.FraudReason) : null,
                AdminNote = (t.Status == "Approved" && t.FraudLog != null) ? t.FraudLog.AdminNote : null,
                ResolvedByAdmin = (t.Status == "Approved" && t.FraudLog != null) ? t.FraudLog.ResolvedByAdmin : null,
                DeclineReason = t.DeclineReason,
                PaymentTypeCode = t.PaymentType.ToString(),
                SenderIBAN = t.SenderIBAN,
                ReceiverIBAN = t.ReceiverIBAN,
                ReceiverName = t.ReceiverName,
                Description = t.Description
            };

            // DTO listelerini oluşturalım ve birleştirelim
            var combinedRecentTxDtos = new List<CardRecentTransactionDto>();

            // 1. Kart İşlemlerini ekle
            if (recentTxList != null)
            {
                combinedRecentTxDtos.AddRange(recentTxList.Select(t => new CardRecentTransactionDto
                {
                    Amount = t.Amount,
                    Currency = t.Currency,
                    Location = t.Location,
                    Country = t.Country,
                    TransactionTypeName = t.TransactionTypeId == 1 ? "Satış İşlemi" : (t.TransactionTypeId == 2 ? "İade İşlemi" : "Transfer İşlemi"),
                    TransactionDate = t.TransactionDate,
                    MerchantCategory = t.MerchantCategory,
                    Status = t.Status,
                    FraudSuspicionReason = (t.Status == "Approved" && t.FraudLog != null) ? (t.FraudLog.FraudRule?.RuleName ?? t.FraudReason) : null,
                    AdminNote = (t.Status == "Approved" && t.FraudLog != null) ? t.FraudLog.AdminNote : null,
                    ResolvedByAdmin = (t.Status == "Approved" && t.FraudLog != null) ? t.FraudLog.ResolvedByAdmin : null,
                    DeclineReason = t.DeclineReason,
                    PaymentTypeCode = t.PaymentType.ToString(),
                    SenderIBAN = t.SenderIBAN,
                    ReceiverIBAN = t.ReceiverIBAN,
                    ReceiverName = t.ReceiverName,
                    Description = t.Description
                }));
            }

            // 2. Gönderilen transferleri ekle
            if (sentTransferList != null)
            {
                combinedRecentTxDtos.AddRange(sentTransferList.Select(t => new CardRecentTransactionDto
                {
                    Amount = t.Amount,
                    Currency = t.Currency,
                    Location = t.Location,
                    Country = t.Country,
                    TransactionTypeName = "Gönderilen Transfer",
                    TransactionDate = t.TransactionDate,
                    MerchantCategory = t.MerchantCategory,
                    Status = t.Status,
                    FraudSuspicionReason = (t.Status == "Approved" && t.FraudLog != null) ? (t.FraudLog.FraudRule?.RuleName ?? t.FraudReason) : null,
                    AdminNote = (t.Status == "Approved" && t.FraudLog != null) ? t.FraudLog.AdminNote : null,
                    ResolvedByAdmin = (t.Status == "Approved" && t.FraudLog != null) ? t.FraudLog.ResolvedByAdmin : null,
                    DeclineReason = t.DeclineReason,
                    PaymentTypeCode = t.PaymentType.ToString(),
                    SenderIBAN = t.SenderIBAN,
                    ReceiverIBAN = t.ReceiverIBAN,
                    ReceiverName = t.ReceiverName,
                    Description = t.Description
                }));
            }

            // 3. Alınan transferleri ekle
            if (receivedTransferList != null)
            {
                combinedRecentTxDtos.AddRange(receivedTransferList.Select(t => new CardRecentTransactionDto
                {
                    Amount = t.Amount,
                    Currency = t.Currency,
                    Location = t.Location,
                    Country = t.Country,
                    TransactionTypeName = "Alınan Transfer",
                    TransactionDate = t.TransactionDate,
                    MerchantCategory = t.MerchantCategory,
                    Status = t.Status,
                    FraudSuspicionReason = (t.Status == "Approved" && t.FraudLog != null) ? (t.FraudLog.FraudRule?.RuleName ?? t.FraudReason) : null,
                    AdminNote = (t.Status == "Approved" && t.FraudLog != null) ? t.FraudLog.AdminNote : null,
                    ResolvedByAdmin = (t.Status == "Approved" && t.FraudLog != null) ? t.FraudLog.ResolvedByAdmin : null,
                    DeclineReason = t.DeclineReason,
                    PaymentTypeCode = t.PaymentType.ToString(),
                    SenderIBAN = t.SenderIBAN,
                    ReceiverIBAN = t.ReceiverIBAN,
                    ReceiverName = t.ReceiverName,
                    Description = t.Description
                }));
            }

            // Tarihe göre sıralayıp son 10'u seçelim
            var finalRecentTransactions = combinedRecentTxDtos
                .OrderByDescending(t => t.TransactionDate)
                .Take(10)
                .ToList();

            var detail = new GetFraudLogDetailResponse
            {
                IsCardSuspicious = isCardSuspicious,
                LogId = logEntity.LogId, 
                TransactionId = logEntity.Transaction?.TransactionId ?? 0,
                Amount = logEntity.Transaction?.Amount ?? 0,
                Currency = logEntity.Transaction?.Currency ?? "TRY",
                TransactionDate = logEntity.Transaction?.TransactionDate ?? System.DateTime.Now,
                Location = logEntity.Transaction?.Location ?? "Bilinmiyor",
                Country = logEntity.Transaction?.Country ?? "Bilinmiyor",
                TransactionTypeName = logEntity.CreditCardTransaction != null 
                    ? (logEntity.CreditCardTransaction.TransactionType?.Description ?? "Satış İşlemi") 
                    : logEntity.DebitCardTransaction != null 
                        ? (logEntity.DebitCardTransaction.TransactionType?.Description ?? "Satış İşlemi") 
                        : "Transfer İşlemi",
                
                MaskedCardNumber = creditCard?.CardNumber ?? debitCard?.CardNumber ?? logEntity.Transaction?.SenderIBAN ?? "Bilinmiyor", 
                CardLimit = creditCard?.CardLimit ?? 0,
                AvailableLimit = creditCard?.AvailableLimit ?? debitCard?.Balance ?? 0,
                IsCardBlocked = creditCard?.IsBlocked ?? debitCard?.IsBlocked ?? false,
                AdminNote = logEntity.AdminNote,
                ResolvedByAdmin = logEntity.ResolvedByAdmin,
                AdminAction = logEntity.AdminAction,
                
                SenderIBAN = logEntity.Transaction?.SenderIBAN ?? debitCard?.IBAN,
                ReceiverIBAN = logEntity.Transaction?.ReceiverIBAN,
                ReceiverName = logEntity.Transaction?.ReceiverName,
                Description = logEntity.Transaction?.Description,
                PaymentTypeCode = logEntity.Transaction?.PaymentType.ToString(),

                CustomerFullName = customer != null ? $"{customer.FirstName} {customer.LastName}" : "Bilinmeyen Müşteri",
                IdentityNumber = customer?.IdentityNumber ?? "Bilinmiyor",
                PhoneNumber = customer?.PhoneNumber ?? "Bilinmiyor", 
                
                RuleName = logEntity.FraudRule?.RuleName ?? "Genel Şüpheli İşlem",
                SuspicionReason = logEntity.Transaction.FraudReason ?? "Sistem tarafından şüpheli bulundu.",
                FraudReason = logEntity.Transaction.FraudReason,
                
                RecentTransactions = finalRecentTransactions,
                RecentSuspiciousTransactions = recentSuspiciousTxList.Select(t => new CardRecentTransactionDto
                {
                    Amount = t.Amount,
                    Currency = t.Currency,
                    Location = t.Location,
                    Country = t.Country,
                    TransactionTypeName = t.TransactionTypeId == 1 ? "Satış İşlemi" : (t.TransactionTypeId == 2 ? "İade İşlemi" : "Transfer İşlemi"),
                    TransactionDate = t.TransactionDate,
                    MerchantCategory = t.MerchantCategory,
                    Status = t.Status,
                    
                    FraudSuspicionReason = (t.Status == "Approved" && t.FraudLog != null) ? (t.FraudLog.FraudRule?.RuleName ?? t.FraudReason) : null,
                    AdminNote = (t.Status == "Approved" && t.FraudLog != null) ? t.FraudLog.AdminNote : null,
                    
                    ResolvedByAdmin = (t.Status == "Approved" && t.FraudLog != null) ? t.FraudLog.ResolvedByAdmin : null,
                    DeclineReason = t.DeclineReason,
                    PaymentTypeCode = t.PaymentType.ToString(),
                    SenderIBAN = t.SenderIBAN,
                    ReceiverIBAN = t.ReceiverIBAN,
                    ReceiverName = t.ReceiverName,
                    Description = t.Description
                }).ToList(),
                RecentSentTransfers = sentTransferList.Select(MapTransferToDto).ToList(),
                RecentReceivedTransfers = receivedTransferList.Select(MapTransferToDto).ToList()
            };

            if (callerRole != UserRoleEnum.Admin)
            {
                detail.MaskedCardNumber = detail.MaskedCardNumber.MaskCardNumber();
                detail.IdentityNumber = detail.IdentityNumber.MaskIdentityNumber();
                detail.PhoneNumber = detail.PhoneNumber.MaskPhoneNumber();
                if (!string.IsNullOrEmpty(detail.SenderIBAN)) detail.SenderIBAN = detail.SenderIBAN.MaskCardNumber();
                if (!string.IsNullOrEmpty(detail.ReceiverIBAN)) detail.ReceiverIBAN = detail.ReceiverIBAN.MaskCardNumber();
            }

            return ResponseDTO<GetFraudLogDetailResponse>.Success(detail);
        }

        public async Task<ResponseDTO<List<GetUnresolvedLogsResponse>>> GetResolvedLogsAsync(UserRoleEnum callerRole)
        {
            var logs = await _fraudLogRepository.GetResolvedLogsAsync();
            var responseList = _mapper.Map<List<GetUnresolvedLogsResponse>>(logs);
            var filteredResolvedList = new List<GetUnresolvedLogsResponse>();

            for (int i = 0; i < responseList.Count; i++)
            {
                var item = responseList[i];
                var originalLog = logs[i]; 

                item.SuspicionReason = originalLog.Transaction?.FraudReason ?? "Sistem tarafından şüpheli bulundu.";
                item.AdminAction = originalLog.AdminAction;
                item.RuleName = originalLog.FraudRule?.RuleName ?? "Genel Şüpheli İşlem";

                var tx = originalLog.Transaction;
                var cc = originalLog.CreditCardTransaction?.CreditCard;
                var dc = originalLog.DebitCardTransaction?.DebitCard;

                // Skor işlem anında motor tarafından hesaplanıp kaydedilmiştir veya FraudReason özetinden okunur.
                int resolvedScore = tx?.RiskScore ?? 0;
                if (!string.IsNullOrEmpty(tx?.FraudReason))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(tx.FraudReason, @"Skor\s+(\d+)");
                    if (match.Success && int.TryParse(match.Groups[1].Value, out int parsedScore))
                    {
                        resolvedScore = Math.Min(100, parsedScore);
                    }
                }
                item.RiskScore = Math.Min(100, resolvedScore);
                item.RiskDecision = RiskDecisionNames.ToWireFormat(tx?.RiskDecision ?? RiskDecisionEnum.Normal);

                if (callerRole == UserRoleEnum.Admin)
                {
                    item.MaskedCardNumber = cc?.CardNumber ?? dc?.CardNumber ?? originalLog.Transaction?.SenderIBAN ?? "Bilinmiyor";
                }

                // 0 - 39 puanlık normal onaylı işlemler fraud geçmişinde yer almaz
                if (resolvedScore >= 40)
                {
                    filteredResolvedList.Add(item);
                }
            }

            return ResponseDTO<List<GetUnresolvedLogsResponse>>.Success(filteredResolvedList);
        }
    }
}
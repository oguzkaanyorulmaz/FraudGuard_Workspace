using AutoMapper;
using FraudGuard.Application.DTOs;
using FraudGuard.Application.DTOs.FraudManagement;
using FraudGuard.Application.Interfaces;
using FraudGuard.Domain.Interfaces.DomainServices;
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
                var cc = originalLog.CreditCardTransaction?.CreditCard;
                var dc = originalLog.DebitCardTransaction?.DebitCard;

                if (tx != null && originalLog.FraudRule != null)
                {
                    decimal limit = cc?.CardLimit ?? 0;
                    decimal available = cc?.AvailableLimit ?? dc?.Balance ?? 0;

                    item.RiskScore = CalculateRiskScore(
                        originalLog.FraudRule.RuleCode,
                        tx
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
                TransactionTypeName = isCredit 
                    ? (logEntity.CreditCardTransaction?.TransactionType?.Description ?? "Bilinmeyen") 
                    : (logEntity.DebitCardTransaction?.TransactionType?.Description ?? "Bilinmeyen"),
                
                MaskedCardNumber = creditCard?.CardNumber ?? debitCard?.CardNumber ?? logEntity.Transaction?.SenderIBAN ?? "Bilinmiyor", 
                CardLimit = creditCard?.CardLimit ?? 0,
                AvailableLimit = creditCard?.AvailableLimit ?? debitCard?.Balance ?? 0,
                IsCardBlocked = creditCard?.IsBlocked ?? debitCard?.IsBlocked ?? false,
                AdminNote = logEntity.AdminNote,
                ResolvedByAdmin = logEntity.ResolvedByAdmin,
                
                CustomerFullName = customer != null ? $"{customer.FirstName} {customer.LastName}" : "Bilinmeyen Müşteri",
                IdentityNumber = customer?.IdentityNumber ?? "Bilinmiyor",
                PhoneNumber = customer?.PhoneNumber ?? "Bilinmiyor", 
                
                RuleName = logEntity.FraudRule?.RuleName ?? "Genel Şüpheli İşlem",
                SuspicionReason = logEntity.Transaction.FraudReason ?? "Sistem tarafından şüpheli bulundu.",
                FraudReason = logEntity.Transaction.FraudReason,
                
                RecentTransactions = recentTxList.Select(t => new CardRecentTransactionDto
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
                    PaymentTypeCode = t.PaymentType.ToString()
                }).ToList(),
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
                    PaymentTypeCode = t.PaymentType.ToString()
                }).ToList()
            };

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
                var cc = originalLog.CreditCardTransaction?.CreditCard;
                var dc = originalLog.DebitCardTransaction?.DebitCard;

                if (tx != null && originalLog.FraudRule != null)
                {
                    decimal limit = cc?.CardLimit ?? 0;
                    decimal available = cc?.AvailableLimit ?? dc?.Balance ?? 0;

                    item.RiskScore = CalculateRiskScore(
                        originalLog.FraudRule.RuleCode,
                        tx
                    );
                }
                else
                {
                    item.RiskScore = 75;
                }
            }

            return ResponseDTO<List<GetUnresolvedLogsResponse>>.Success(responseList);
        }

        private int CalculateRiskScore(string ruleCode, ITransaction tx)
        {
            // 1. Baz Kural Ağırlığı
            int ruleWeight = ruleCode switch
            {
                "IMPOSSIBLE_TRAVEL" => 95,
                "BRUTE_FORCE" => 90,
                "MAX_OUT" => 85,
                "ANOMALOUS_TIME" => 85,
                "HIGH_RISK_RECEIVER" => 85,
                "WALLET_CASHOUT" => 80,
                "NEW_BENEFICIARY_TRANSFER" => 75,
                "CARD_TESTING" => 75,
                "CROSS_BORDER" => 70,
                "CROSS_BORDER_TRANSFER" => 70,
                "HIGH_RISK_MCC" => 65,
                "MULTI_SOURCE_FUNDING" => 65,
                "RECEIVER_BALANCE_ANOMALY" => 60,
                "CONSECUTIVE_REFUNDS" => 60,
                "CURRENCY_MISMATCH" => 55,
                "VELOCITY" => 50,
                "SMURFING" => 50,
                "SUSPICIOUS_DESCRIPTION" => 45,
                _ => 60
            };

            // 2. Kanal Çarpanı
            decimal channelFactor = tx.ChannelTypeId switch
            {
                2 => 1.3m, // Sanal POS
                3 => 1.2m, // ATM
                1 => 1.0m, // POS
                4 => 0.85m, // Mobil Şube
                5 => 0.85m, // İnternet Şubesi
                _ => 1.0m
            };

            // 3. İşyeri Kategorisi Çarpanı
            decimal categoryFactor = 1.0m;
            if (!string.IsNullOrEmpty(tx.MerchantCategory))
            {
                string category = tx.MerchantCategory.ToLower();
                if (category.Contains("kuyumcu") || category.Contains("kripto") || category.Contains("bahis") || category.Contains("kumar"))
                    categoryFactor = 1.4m;
                else if (category.Contains("elektronik") || category.Contains("seyahat") || category.Contains("otel") || category.Contains("konaklama"))
                    categoryFactor = 1.2m;
                else if (category.Contains("e-ticaret") || category.Contains("transfer"))
                    categoryFactor = 1.1m;
                else if (category.Contains("market") || category.Contains("giyim") || category.Contains("restoran") || category.Contains("yemek"))
                    categoryFactor = 0.8m;
            }

            // 4. İşlem Tipi Çarpanı
            decimal typeFactor = tx.TransactionTypeId switch
            {
                2 => 1.2m, // Refund (İade)
                3 => 0.7m, // Void (İptal)
                _ => 1.0m  // Sale / Transfer
            };

            // 5. Para Birimi Çarpanı
            decimal currencyFactor = (!string.IsNullOrEmpty(tx.Currency) && tx.Currency != "TRY") ? 1.2m : 1.0m;

            // 6. Konum/Ülke Çarpanı
            decimal countryFactor = (!string.IsNullOrEmpty(tx.Country) && tx.Country.ToLower() != "türkiye") ? 1.3m : 1.0m;

            // Katsayıların Bileşkesi
            decimal combinedFactor = channelFactor * categoryFactor * typeFactor * currencyFactor * countryFactor;

            // 7. Bakiye / Limit Oranı Hesaplama
            decimal limitOranEtkisi = 0;
            decimal cardLimit = 0;
            decimal availableLimit = 0;

            if (tx is ECreditCardTransaction ccTx && ccTx.CreditCard != null)
            {
                cardLimit = ccTx.CreditCard.CardLimit;
                availableLimit = ccTx.CreditCard.AvailableLimit;
            }
            else if (tx is EDebitCardTransaction dcTx && dcTx.DebitCard != null)
            {
                cardLimit = 100000; // Varsayılan limit eşiği
                availableLimit = dcTx.DebitCard.Balance;
            }

            if (cardLimit > 0)
            {
                decimal spentLimit = System.Math.Max(0, cardLimit - availableLimit);
                decimal txRatio = (tx.Amount / cardLimit) * 100;
                decimal spentRatio = (spentLimit / cardLimit) * 100;
                limitOranEtkisi = (txRatio * 0.6m) + (spentRatio * 0.4m);
                if (limitOranEtkisi > 100) limitOranEtkisi = 100;
            }

            // Hacim Skoru (Tek işlem tutarının büyüklüğü)
            decimal volumeScore = System.Math.Min((tx.Amount / 50000m) * 100m, 100m);

            // Dinamik Faktör (%50 Limit Kullanım Oranı + %50 İşlem Hacmi)
            decimal dynamicFactor = ((limitOranEtkisi * 0.5m) + (volumeScore * 0.5m)) / 100m;

            // Risk Skoru Hesaplama
            decimal rawScore = (ruleWeight * combinedFactor) + (100m - ruleWeight) * dynamicFactor;

            int finalScore = (int)System.Math.Round(rawScore);
            return System.Math.Clamp(finalScore, 1, 100);
        }
    }
}
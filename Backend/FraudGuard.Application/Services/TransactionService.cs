using FraudGuard.Domain.Common.Constants;
using FraudGuard.Domain.Common.Enums;
using FraudGuard.Domain.DomainObjects.FraudEvaluation;
using FraudGuard.Domain.DomainObjects.TransactionProcessing;
using FraudGuard.Domain.Entities;
using FraudGuard.Domain.Interfaces.Abstractions;
using FraudGuard.Application.Interfaces;
using FraudGuard.Domain.Interfaces.DomainServices;
using FraudGuard.Domain.Interfaces.Repositories;
using System;
using System.Threading.Tasks;

namespace FraudGuard.Application.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly ICreditCardRepository _creditCardRepository;
        private readonly IDebitCardRepository _debitCardRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly IFraudEvaluationService _fraudEvaluationService;
        private readonly IBankAccountBeneficiaryRepository _bankAccountBeneficiaryRepository;
        private readonly ICurrencyService _currencyService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheProvider _cacheProvider;

        public TransactionService(
            ICreditCardRepository creditCardRepository,
            IDebitCardRepository debitCardRepository,
            ITransactionRepository transactionRepository,
            IFraudEvaluationService fraudEvaluationService,
            IBankAccountBeneficiaryRepository bankAccountBeneficiaryRepository,
            ICurrencyService currencyService,
            IUnitOfWork unitOfWork,
            ICacheProvider cacheProvider)
        {
            _creditCardRepository = creditCardRepository;
            _debitCardRepository = debitCardRepository;
            _transactionRepository = transactionRepository;
            _fraudEvaluationService = fraudEvaluationService;
            _bankAccountBeneficiaryRepository = bankAccountBeneficiaryRepository;
            _currencyService = currencyService;
            _unitOfWork = unitOfWork;
            _cacheProvider = cacheProvider;
        }

        /// <summary>EBlockReason tablosundaki "Fraud" (Dolandırıcılık Şüphesi) kaydının kimliği.</summary>
        private const int FraudBlockReasonId = 2;

        private string GenerateRrn()
        {
            return DateTime.UtcNow.ToString("yyMMdd") + new Random().Next(100000, 999999).ToString("D6");
        }

        /// <summary>
        /// Fraud motorunun kademeli kararını işlem sonucuna yansıtır.
        /// RET_BLOKE reddedilir; İZLE ve EK_DOGRULAMA şüpheli olarak kaydedilip analiste düşer.
        /// </summary>
        private static void ApplyFraudDecision(TransactionCheckResult result, FraudDecisionResult evaluation)
        {
            result.FraudDecision = evaluation;
            result.IsSuspicious = evaluation.IsSuspicious;
            result.TriggeredRuleName = evaluation.PrimaryRule?.RuleName ?? string.Empty;

            if (!evaluation.IsSuspicious)
                return;

            // NORMAL kademesi "işleme izin ver" demektir. Kural tetiklenmiş olsa bile
            // skor eşiğin altındaysa işlem şüpheli işaretlenmez; yalnızca gerekçe kaydedilir.
            if (evaluation.Decision == RiskDecisionEnum.Normal)
                return;

            if (evaluation.ShouldBlock)
            {
                result.Status = "Declined";
                result.DeclineReason = $"Fraud RET — Risk skoru {evaluation.FinalRiskScore}";
                return;
            }

            result.Status = "Suspicious";
            result.DeclineReason = evaluation.Decision == RiskDecisionEnum.EkDogrulama
                ? $"Ek doğrulama gerekli (3D/OTP) — Risk skoru {evaluation.FinalRiskScore}"
                : $"Fraud Şüphesi — Risk skoru {evaluation.FinalRiskScore}";
        }

        /// <summary>
        /// RET_BLOKE kararında kartı fraud gerekçesiyle bloke eder.
        /// </summary>
        private async Task BlockCardForFraudAsync(ECreditCard? creditCard, EDebitCard? debitCard)
        {
            if (creditCard != null)
            {
                creditCard.IsBlocked = true;
                creditCard.BlockReasonId = FraudBlockReasonId;
                await _creditCardRepository.UpdateAsync(creditCard);
            }
            else if (debitCard != null)
            {
                debitCard.IsBlocked = true;
                debitCard.BlockReasonId = FraudBlockReasonId;
                await _debitCardRepository.UpdateAsync(debitCard);
            }
        }

        public async Task<TransactionCheckResult> ProcessTransactionAsync(ProcessTransactionInput input)
        {
            var result = new TransactionCheckResult();

            if (input.PaymentType == PaymentTypeEnum.EFT || input.PaymentType == PaymentTypeEnum.BankTransfer)
            {
                var senderDebit = await _debitCardRepository.GetByIBANAsync(input.SenderIBAN);
                if (senderDebit == null)
                {
                    result.Status = "Declined";
                    result.DeclineReason = "Gönderici hesap bulunamadı.";
                    return result;
                }

                if (senderDebit.IsBlocked)
                {
                    result.Status = "Declined";
                    result.DeclineReason = "Gönderen kart/hesap blokeli.";
                    return result;
                }

                // Not: "bekleyen şüpheli işlem varsa tümünü reddet" kontrolü kaldırıldı.
                // Kademeli karar modelinde İZLE "işleme izin ver, analiste bayrak düş" demektir;
                // eski kontrol İZLE'yi fiilen kalıcı bloke haline getiriyordu.
                // Gerçek bloke RET_BLOKE kararında uygulanır (kart/hesap IsBlocked işaretlenir)
                // ve yukarıdaki IsBlocked kontrolü tarafından yakalanır.

                decimal processedAmount = await _currencyService.ConvertToTryAsync(input.Amount, input.Currency);

                if (senderDebit.Balance < processedAmount)
                {
                    result.Status = "Declined";
                    result.DeclineReason = "Yetersiz Bakiye.";
                    return result;
                }

                var receiverDebit = await _debitCardRepository.GetByIBANAsync(input.ReceiverIBAN);
                if (receiverDebit == null && input.PaymentType == PaymentTypeEnum.BankTransfer)
                {
                    result.Status = "Declined";
                    result.DeclineReason = "Alıcı hesap bulunamadı.";
                    return result;
                }

                if (receiverDebit != null)
                {
                    string dbFullName = $"{receiverDebit.Customer.FirstName} {receiverDebit.Customer.LastName}".Trim();
                    string inputFullName = (input.ReceiverName ?? "").Trim();
                    
                    if (!string.Equals(dbFullName, inputFullName, StringComparison.CurrentCultureIgnoreCase))
                    {
                        result.Status = "Declined";
                        result.DeclineReason = "Alıcı adı ve IBAN uyuşmuyor.";
                        return result;
                    }
                }

                var evaluationResult = await _fraudEvaluationService.EvaluateAsync(
                    input, senderDebit.CardId, isCreditCard: false);
                ApplyFraudDecision(result, evaluationResult);
                bool isSuspicious = evaluationResult.IsSuspicious;

                if (evaluationResult.ShouldBlock)
                {
                    // RET kararı: tutar aktarılmaz, gönderen hesap fraud gerekçesiyle bloke edilir.
                    await BlockCardForFraudAsync(null, senderDebit);
                }
                else if (evaluationResult.Decision != RiskDecisionEnum.Normal)
                {
                    // İZLE / EK_DOĞRULAMA: tutar gönderenden düşülür ancak alıcıya geçmez,
                    // inceleme sonuçlanana kadar bekletilir.
                    senderDebit.Balance -= processedAmount;
                    await _debitCardRepository.UpdateAsync(senderDebit);
                }
                else
                {
                    senderDebit.Balance -= processedAmount;
                    if (receiverDebit != null)
                    {
                        receiverDebit.Balance += processedAmount;
                        await _debitCardRepository.UpdateAsync(receiverDebit);
                    }
                    await _debitCardRepository.UpdateAsync(senderDebit);
                    result.Status = "Approved";

                    bool hasBeneficiary = await _bankAccountBeneficiaryRepository.AnyAsync(senderDebit.CustomerId, input.ReceiverIBAN);
                    if (!hasBeneficiary)
                    {
                        var beneficiary = new EBankAccountBeneficiary
                        {
                            CustomerId = senderDebit.CustomerId,
                            ReceiverIBAN = input.ReceiverIBAN,
                            ReceiverName = input.ReceiverName ?? "Alıcı",
                            AddedDate = DateTime.Now
                        };
                        await _bankAccountBeneficiaryRepository.AddAsync(beneficiary);
                    }
                }

                var transferTx = new ETransferTransaction
                {
                    RRN = GenerateRrn(),
                    SenderIBAN = input.SenderIBAN,
                    ReceiverIBAN = input.ReceiverIBAN,
                    ReceiverName = input.ReceiverName,
                    Description = input.Description,
                    ChannelTypeId = input.ChannelTypeId,
                    Amount = input.Amount,
                    Currency = input.Currency,
                    TransactionDate = DateTime.Now,
                    Location = input.Location ?? "İnternet Bankacılığı",
                    Country = input.Country ?? "Türkiye",
                    Status = result.Status,
                    DeclineReason = result.DeclineReason,
                    FraudReason = evaluationResult.BuildReasonSummary(),
                    RiskScore = evaluationResult.FinalRiskScore,
                    RiskDecision = evaluationResult.Decision
                };

                await _transactionRepository.AddTransferTransactionAsync(transferTx);
                await _unitOfWork.SaveChangesAsync();
                await _cacheProvider.RemoveAsync($"card_info_{senderDebit.CardNumber}");
                await _cacheProvider.RemoveAsync($"recent_txs_{senderDebit.CardNumber}");
                await _cacheProvider.RemoveAsync($"recent_txs_{input.SenderIBAN}");
                if (receiverDebit != null)
                {
                    await _cacheProvider.RemoveAsync($"card_info_{receiverDebit.CardNumber}");
                    await _cacheProvider.RemoveAsync($"recent_txs_{receiverDebit.CardNumber}");
                }

                // Alarm yalnızca karar NORMAL'in üzerindeyse açılır; onaylanan işlem
                // analist kuyruğuna girmez. Status, ApplyFraudDecision tarafından kademeye
                // göre set edilmiştir.
                if (evaluationResult.PrimaryRule != null && result.Status != "Approved")
                {
                    bool isAutoBlocked = evaluationResult.ShouldBlock || result.Status == "Declined";
                    await _fraudEvaluationService.CreateFraudLogAsync(
                        transferTx.TransactionId,
                        evaluationResult.PrimaryRule.RuleCode,
                        input.PaymentType,
                        isAutoBlocked: isAutoBlocked,
                        resolvedBy: isAutoBlocked ? "Sistem (Otomatik Bloke)" : null,
                        adminNote: isAutoBlocked ? "Sistem tarafından şüpheli bulunup direkt bloke edildi." : null);
                    await _unitOfWork.SaveChangesAsync();
                }

                result.TransactionId = transferTx.TransactionId;
                result.RRN = transferTx.RRN;
                return result;
            }
            else
            {
                string cacheKey = $"card_info_{input.CardNumber}";
                var cachedCard = await _cacheProvider.GetAsync<FraudGuard.Domain.DomainObjects.CardCacheInfo>(cacheKey);

                if (cachedCard == null)
                {
                    var cc = await _creditCardRepository.GetByCardNumberAsync(input.CardNumber);
                    if (cc != null)
                    {
                        cachedCard = new FraudGuard.Domain.DomainObjects.CardCacheInfo { AvailableFunds = cc.AvailableLimit, IsBlocked = cc.IsBlocked, CVV = cc.CVV };
                    }
                    else
                    {
                        var dc = await _debitCardRepository.GetByCardNumberAsync(input.CardNumber);
                        if (dc != null)
                        {
                            cachedCard = new FraudGuard.Domain.DomainObjects.CardCacheInfo { AvailableFunds = dc.Balance, IsBlocked = dc.IsBlocked, CVV = dc.CVV };
                        }
                    }
                    if (cachedCard != null)
                    {
                        await _cacheProvider.SetAsync(cacheKey, cachedCard, TimeSpan.FromMinutes(5));
                    }
                }

                if (cachedCard == null)
                {
                    result.Status = "Declined";
                    result.DeclineReason = "Geçersiz Kart";
                    return result;
                }

                if (cachedCard.IsBlocked)
                {
                    result.Status = "Declined";
                    result.DeclineReason = "Kart Blokeli";
                    return result;
                }

                var creditCard = await _creditCardRepository.GetByCardNumberAsync(input.CardNumber);
                var debitCard = await _debitCardRepository.GetByCardNumberAsync(input.CardNumber);
                bool isCredit = creditCard != null;
                int cardId = isCredit ? creditCard.CardId : debitCard.CardId;
                bool isBlocked = isCredit ? creditCard.IsBlocked : debitCard.IsBlocked;
                string cardCvv = isCredit ? creditCard.CVV : debitCard.CVV;

                // Not: "geçmişte şüpheli işlem varsa tümünü reddet" kontrolü kaldırıldı.
                // Bkz. transfer dalındaki açıklama — bloke yalnızca RET_BLOKE kararında uygulanır.

                bool isCvvIncorrect = (cardCvv != input.CVV);
                bool isCvvSuspicious = false;

                if (isBlocked)
                {
                    result.Status = "Declined";
                    result.DeclineReason = "Kart Blokeli";
                }
                else if (isCvvIncorrect) 
                {
                    string cvvFailKey = $"cvv_fail_cnt_{input.CardNumber}";
                    int failCount = (await _cacheProvider.GetAsync<int>(cvvFailKey)) + 1;
                    await _cacheProvider.SetAsync(cvvFailKey, failCount, TimeSpan.FromMinutes(30));

                    if (failCount >= 3)
                    {
                        isCvvSuspicious = true;
                        result.Status = "Suspicious";
                        result.DeclineReason = "Fraud Şüphesi: BRUTE_FORCE";
                        await _cacheProvider.RemoveAsync(cvvFailKey);
                    }
                    else
                    {
                        result.Status = "Declined";
                        result.DeclineReason = "Hatalı CVV";
                    }
                }
                else
                {
                    result.Status = "Approved";
                    string cvvFailKey = $"cvv_fail_cnt_{input.CardNumber}";
                    await _cacheProvider.RemoveAsync(cvvFailKey);
                }

                decimal processedAmount = await _currencyService.ConvertToTryAsync(input.Amount, input.Currency);

                bool isSuspicious = isCvvSuspicious;
                string? triggeredRuleCode = isCvvSuspicious ? "BRUTE_FORCE" : null;
                string? capturedFraudReason = isCvvSuspicious ? "3 kez üst üste hatalı CVV denemesi yapılmıştır." : null;

                // CVV brute-force kontrolü kural motorundan önce, ondan bağımsız çalışır;
                // dolayısıyla motorun ürettiği bir skoru yoktur. Mevcut davranışı (analiste
                // düşen şüpheli işlem) korumak için İZLE eşiğine sabitlenir.
                int capturedRiskScore = isCvvSuspicious ? RiskScoringConstants.IzleThreshold : 0;
                RiskDecisionEnum capturedDecision = isCvvSuspicious ? RiskDecisionEnum.Izle : RiskDecisionEnum.Normal;

                if (input.TransactionType == TransactionTypeEnum.Refund) 
                {
                    if (string.IsNullOrEmpty(input.RRN))
                    {
                        result.Status = "Declined";
                        result.DeclineReason = "İade işlemi için RRN değeri belirtilmelidir.";
                        return result;
                    }

                    var originalTx = await _transactionRepository.GetOriginalSaleByRrnAsync(input.RRN, cardId, isCredit);
                    if (originalTx == null)
                    {
                        result.Status = "Declined";
                        result.DeclineReason = "Orijinal satış işlemi bulunamadı.";
                        return result;
                    }

                    bool alreadyRefunded = await _transactionRepository.HasBeenRefundedAsync(input.RRN, cardId, isCredit);
                    if (alreadyRefunded)
                    {
                        result.Status = "Declined";
                        result.DeclineReason = "Bu işlem zaten iade edilmiştir.";
                        return result;
                    }

                    if (originalTx.Amount != input.Amount || originalTx.Currency != input.Currency)
                    {
                        result.Status = "Declined";
                        result.DeclineReason = "İade tutarı veya para birimi orijinal işlem ile eşleşmiyor.";
                        return result;
                    }

                    if (isCredit)
                    {
                        creditCard.AvailableLimit = Math.Min(creditCard.AvailableLimit + processedAmount, creditCard.CardLimit);
                        await _creditCardRepository.UpdateAsync(creditCard);
                    }
                    else
                    {
                        debitCard.Balance += processedAmount;
                        await _debitCardRepository.UpdateAsync(debitCard);
                    }
                    await _cacheProvider.RemoveAsync(cacheKey);
                    await _cacheProvider.RemoveAsync($"recent_txs_{input.CardNumber}");

                    if (!isCvvSuspicious)
                    {
                        var evaluationResult = await _fraudEvaluationService.EvaluateAsync(input, cardId, isCredit);
                        triggeredRuleCode = evaluationResult.PrimaryRule?.RuleCode;
                        capturedFraudReason = evaluationResult.BuildReasonSummary();
                        isSuspicious = evaluationResult.IsSuspicious;
                        capturedRiskScore = evaluationResult.FinalRiskScore;
                        capturedDecision = evaluationResult.Decision;

                        result.Status = "Approved";
                        ApplyFraudDecision(result, evaluationResult);

                        if (evaluationResult.ShouldBlock)
                        {
                            await BlockCardForFraudAsync(isCredit ? creditCard : null, isCredit ? null : debitCard);
                        }
                    }
                }
                else if (input.TransactionType == TransactionTypeEnum.Sale)
                {
                    bool isInitialDecline = result.Status == "Declined";

                    decimal availableFunds = isCredit ? creditCard.AvailableLimit : debitCard.Balance;
                    if (!isInitialDecline && !isCvvSuspicious && availableFunds < processedAmount)
                    {
                        result.Status = "Declined";
                        result.DeclineReason = "Yetersiz Bakiye";
                        isInitialDecline = true;
                    }

                    if (!isBlocked && !isCvvSuspicious && !isInitialDecline)
                    {
                        var evaluationResult = await _fraudEvaluationService.EvaluateAsync(input, cardId, isCredit);

                        triggeredRuleCode = evaluationResult.PrimaryRule?.RuleCode;
                        capturedFraudReason = evaluationResult.BuildReasonSummary();
                        isSuspicious = evaluationResult.IsSuspicious;
                        capturedRiskScore = evaluationResult.FinalRiskScore;
                        capturedDecision = evaluationResult.Decision;

                        result.Status = "Approved";
                        ApplyFraudDecision(result, evaluationResult);

                        if (evaluationResult.ShouldBlock)
                        {
                            // RET kararında tutar tahsil edilmez ve kart bloke edilir.
                            await BlockCardForFraudAsync(isCredit ? creditCard : null, isCredit ? null : debitCard);
                        }
                        else if (isCredit)
                        {
                            creditCard.AvailableLimit -= processedAmount;
                        }
                        else
                        {
                            debitCard.Balance -= processedAmount;
                        }
                    }
                }

                else if (input.TransactionType == TransactionTypeEnum.Deposit)
                {
                    if (isCredit)
                    {
                        result.Status = "Declined";
                        result.DeclineReason = "Kredi kartına doğrudan para yatırılamaz. Lütfen Kredi Kartı Borç Ödeme seçeneğini kullanın.";
                        return result;
                    }

                    // Banka kartı bakiyesini yatırılan tutar kadar arttır
                    debitCard.Balance += processedAmount;
                    await _debitCardRepository.UpdateAsync(debitCard);
                    
                    result.Status = "Approved"; // Para yatırma işlemi varsayılan olarak onaylanır
                    await _cacheProvider.RemoveAsync(cacheKey);
                    await _cacheProvider.RemoveAsync($"recent_txs_{input.CardNumber}");

                    // 💥 FRAUD DEĞERLENDİRMESİ
                    if (!isBlocked && !isCvvSuspicious)
                    {
                        var evaluationResult = await _fraudEvaluationService.EvaluateAsync(input, cardId, isCredit);
                        triggeredRuleCode = evaluationResult.PrimaryRule?.RuleCode;
                        capturedFraudReason = evaluationResult.BuildReasonSummary();
                        isSuspicious = evaluationResult.IsSuspicious;
                        capturedRiskScore = evaluationResult.FinalRiskScore;
                        capturedDecision = evaluationResult.Decision;

                        ApplyFraudDecision(result, evaluationResult);

                        if (evaluationResult.ShouldBlock)
                        {
                            await BlockCardForFraudAsync(isCredit ? creditCard : null, isCredit ? null : debitCard);
                        }
                    }
                }

                

                else if (input.TransactionType == TransactionTypeEnum.CardPayment)
                {
                    if (!isCredit)
                    {
                        result.Status = "Declined";
                        result.DeclineReason = "Banka kartı için borç ödeme işlemi yapılamaz.";
                        return result;
                    }

                    // Borç = Kart Limiti - Mevcut Kullanılabilir Limit
                    decimal currentDebt = creditCard.CardLimit - creditCard.AvailableLimit;

                    // Eğer yatırılmak istenen tutar mevcut borçtan fazlaysa işlemi doğrudan iptal et ve uyarı ver
                    if (processedAmount > currentDebt)
                    {
                        result.Status = "Declined";
                        result.DeclineReason = $"Borcunuz {currentDebt:N2} {input.Currency}'dir, fazla ödeme yapmayı denediniz.";
                        return result;
                    }

                    // Borç ödemesini gerçekleştir (Kullanılabilir limiti arttır)
                    creditCard.AvailableLimit += processedAmount;
                    await _creditCardRepository.UpdateAsync(creditCard);

                    result.Status = "Approved"; // Borç ödeme onaylanır
                    await _cacheProvider.RemoveAsync(cacheKey);
                    await _cacheProvider.RemoveAsync($"recent_txs_{input.CardNumber}");

                    // 💥 FRAUD DEĞERLENDİRMESİ
                    if (!isBlocked && !isCvvSuspicious)
                    {
                        var evaluationResult = await _fraudEvaluationService.EvaluateAsync(input, cardId, isCredit);
                        triggeredRuleCode = evaluationResult.PrimaryRule?.RuleCode;
                        capturedFraudReason = evaluationResult.BuildReasonSummary();
                        isSuspicious = evaluationResult.IsSuspicious;
                        capturedRiskScore = evaluationResult.FinalRiskScore;
                        capturedDecision = evaluationResult.Decision;

                        ApplyFraudDecision(result, evaluationResult);

                        if (evaluationResult.ShouldBlock)
                        {
                            await BlockCardForFraudAsync(isCredit ? creditCard : null, isCredit ? null : debitCard);
                        }
                    }
                }






                int newTransactionId = 0;
                string assignedRrn = input.TransactionType == TransactionTypeEnum.Refund ? input.RRN : GenerateRrn();

                if (isCredit)
                {
                    var ccTx = new ECreditCardTransaction
                    {
                        CreditCardId = cardId,
                        TransactionTypeId = (int)input.TransactionType,
                        ChannelTypeId = input.ChannelTypeId == 0 ? 2 : input.ChannelTypeId,
                        Amount = input.Amount,
                        Currency = input.Currency,
                        TransactionDate = DateTime.Now,
                        Location = input.Location,
                        Country = input.Country,
                        MerchantCategory = input.MerchantCategory,
                        MerchantId = input.MerchantId,
                        Status = result.Status,
                        DeclineReason = result.Status == "Suspicious" ? $"Fraud: {triggeredRuleCode}" : result.DeclineReason,
                        FraudReason = capturedFraudReason,
                        RiskScore = capturedRiskScore,
                        RiskDecision = capturedDecision,
                        RRN = assignedRrn
                    };

                    await _transactionRepository.AddCreditCardTransactionAsync(ccTx);
                    await _creditCardRepository.UpdateAsync(creditCard);
                    await _unitOfWork.SaveChangesAsync();
                    newTransactionId = ccTx.TransactionId;
                }
                else
                {
                    var dcTx = new EDebitCardTransaction
                    {
                        DebitCardId = cardId,
                        TransactionTypeId = (int)input.TransactionType,
                        ChannelTypeId = input.ChannelTypeId == 0 ? 2 : input.ChannelTypeId,
                        Amount = input.Amount,
                        Currency = input.Currency,
                        TransactionDate = DateTime.Now,
                        Location = input.Location,
                        Country = input.Country,
                        MerchantCategory = input.MerchantCategory,
                        MerchantId = input.MerchantId,
                        Status = result.Status,
                        DeclineReason = result.Status == "Suspicious" ? $"Fraud: {triggeredRuleCode}" : result.DeclineReason,
                        FraudReason = capturedFraudReason,
                        RiskScore = capturedRiskScore,
                        RiskDecision = capturedDecision,
                        RRN = assignedRrn
                    };

                    await _transactionRepository.AddDebitCardTransactionAsync(dcTx);
                    await _debitCardRepository.UpdateAsync(debitCard);
                    await _unitOfWork.SaveChangesAsync();
                    newTransactionId = dcTx.TransactionId;
                }
                
                await _cacheProvider.RemoveAsync(cacheKey);
                await _cacheProvider.RemoveAsync($"recent_txs_{input.CardNumber}");

                // Bkz. transfer dalı: NORMAL kademesinde alarm açılmaz.
                if (triggeredRuleCode != null && result.Status != "Approved")
                {
                    bool isAutoBlocked = (capturedDecision == RiskDecisionEnum.RetBloke || result.Status == "Declined");
                    await _fraudEvaluationService.CreateFraudLogAsync(
                        newTransactionId,
                        triggeredRuleCode,
                        input.PaymentType,
                        isAutoBlocked: isAutoBlocked,
                        resolvedBy: isAutoBlocked ? "Sistem (Otomatik Bloke)" : null,
                        adminNote: isAutoBlocked ? "Sistem tarafından şüpheli bulunup direkt bloke edildi." : null);
                    await _unitOfWork.SaveChangesAsync();
                }

                result.TransactionId = newTransactionId;
                result.RRN = assignedRrn;
                return result;
            }
        }
    }
}

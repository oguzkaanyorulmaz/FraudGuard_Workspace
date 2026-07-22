using FraudGuard.Domain.Common.Enums;
using FraudGuard.Domain.DomainObjects.TransactionProcessing;
using FraudGuard.Domain.Entities;
using FraudGuard.Domain.Interfaces.Abstractions;
using FraudGuard.Domain.Interfaces.DomainServices;
using FraudGuard.Domain.Interfaces.Repositories;
using System;
using System.Threading.Tasks;

namespace FraudGuard.Domain.Services
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

        private string GenerateRrn()
        {
            return DateTime.UtcNow.ToString("yyMMdd") + new Random().Next(100000, 999999).ToString("D6");
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

                bool hasSuspicious = await _transactionRepository.HasAnySuspiciousTransactionAsync(senderDebit.CardId, isCreditCard: false);
                if (hasSuspicious)
                {
                    result.Status = "Declined";
                    result.DeclineReason = "Hesabınızda bekleyen şüpheli işlem bulunmaktadır. Yeni işlem yapılamaz.";
                    return result;
                }

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

                var evaluationResult = await _fraudEvaluationService.EvaluateAsync(input, senderDebit.CardId);
                bool isSuspicious = !string.IsNullOrEmpty(evaluationResult.RuleCode);

                if (isSuspicious)
                {
                    result.Status = "Suspicious";
                    result.DeclineReason = $"Fraud Şüphesi: {evaluationResult.RuleCode}";

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
                    FraudReason = evaluationResult.FraudReason
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

                if (isSuspicious && evaluationResult.RuleCode != null)
                {
                    await _fraudEvaluationService.CreateFraudLogAsync(transferTx.TransactionId, evaluationResult.RuleCode, input.PaymentType);
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

                bool hasSuspicious = await _transactionRepository.HasAnySuspiciousTransactionAsync(cardId, isCreditCard: isCredit);
                if (hasSuspicious)
                {
                    result.Status = "Declined";
                    result.DeclineReason = "Kart şüpheli durumda, müşteri hizmetlerini arayınız.";
                    return result;
                }

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
                        var evaluationResult = await _fraudEvaluationService.EvaluateAsync(input, cardId);
                        triggeredRuleCode = evaluationResult.RuleCode;
                        capturedFraudReason = evaluationResult.FraudReason;
                        
                        isSuspicious = !string.IsNullOrEmpty(triggeredRuleCode);
                        result.Status = isSuspicious ? "Suspicious" : "Approved";
                        if (isSuspicious)
                        {
                            result.DeclineReason = $"Fraud Şüphesi: {triggeredRuleCode}";
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
                        var evaluationResult = await _fraudEvaluationService.EvaluateAsync(input, cardId);
                        
                        triggeredRuleCode = evaluationResult.RuleCode;
                        capturedFraudReason = evaluationResult.FraudReason;
                        
                        isSuspicious = !string.IsNullOrEmpty(triggeredRuleCode);
                        if (isInitialDecline)
                        {
                            result.Status = "Declined";
                        }
                        else
                        {
                            if (isSuspicious)
                            {
                                result.Status = "Suspicious";
                                result.DeclineReason = $"Fraud Şüphesi: {triggeredRuleCode}";
                            }
                            else
                            {
                                result.Status = "Approved";
                            }

                            if (isCredit)
                            {
                                creditCard.AvailableLimit -= processedAmount;
                            }
                            else
                            {
                                debitCard.Balance -= processedAmount;
                            }
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
                        Status = result.Status,
                        DeclineReason = result.Status == "Suspicious" ? $"Fraud: {triggeredRuleCode}" : result.DeclineReason,
                        FraudReason = capturedFraudReason,
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
                        Status = result.Status,
                        DeclineReason = result.Status == "Suspicious" ? $"Fraud: {triggeredRuleCode}" : result.DeclineReason,
                        FraudReason = capturedFraudReason,
                        RRN = assignedRrn
                    };

                    await _transactionRepository.AddDebitCardTransactionAsync(dcTx);
                    await _debitCardRepository.UpdateAsync(debitCard);
                    await _unitOfWork.SaveChangesAsync();
                    newTransactionId = dcTx.TransactionId;
                }
                
                await _cacheProvider.RemoveAsync(cacheKey);
                await _cacheProvider.RemoveAsync($"recent_txs_{input.CardNumber}");

                if (isSuspicious && triggeredRuleCode != null)
                {
                    await _fraudEvaluationService.CreateFraudLogAsync(newTransactionId, triggeredRuleCode, input.PaymentType);
                    await _unitOfWork.SaveChangesAsync(); 
                }

                result.TransactionId = newTransactionId;
                result.RRN = assignedRrn;
                return result;
            }
        }
    }
}

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

        public TransactionService(
            ICreditCardRepository creditCardRepository,
            IDebitCardRepository debitCardRepository,
            ITransactionRepository transactionRepository,
            IFraudEvaluationService fraudEvaluationService,
            IBankAccountBeneficiaryRepository bankAccountBeneficiaryRepository,
            ICurrencyService currencyService,
            IUnitOfWork unitOfWork)
        {
            _creditCardRepository = creditCardRepository;
            _debitCardRepository = debitCardRepository;
            _transactionRepository = transactionRepository;
            _fraudEvaluationService = fraudEvaluationService;
            _bankAccountBeneficiaryRepository = bankAccountBeneficiaryRepository;
            _currencyService = currencyService;
            _unitOfWork = unitOfWork;
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

                
                var transferTx = new ETransaction
                {
                    DebitCardId = senderDebit.CardId,
                    SenderIBAN = input.SenderIBAN,
                    ReceiverIBAN = input.ReceiverIBAN,
                    ReceiverName = input.ReceiverName,
                    Description = input.Description,
                    TransactionTypeId = 4,
                    PaymentType = input.PaymentType,
                    ChannelTypeId = input.ChannelTypeId,
                    Amount = input.Amount,
                    Currency = input.Currency,
                    TransactionDate = DateTime.Now,
                    Location = input.Location ?? "İnternet Bankacılığı",
                    Country = input.Country ?? "Türkiye",
                    MerchantCategory = "Para Transferi",
                    Status = result.Status,
                    DeclineReason = result.DeclineReason,
                    FraudReason = evaluationResult.FraudReason
                };

                await _transactionRepository.AddAsync(transferTx);
                await _unitOfWork.SaveChangesAsync();

                if (isSuspicious && evaluationResult.RuleCode != null)
                {
                    await _fraudEvaluationService.CreateFraudLogAsync(transferTx.TransactionId, evaluationResult.RuleCode);
                    await _unitOfWork.SaveChangesAsync();
                }

                result.TransactionId = transferTx.TransactionId;
                return result;
            }

            else
            {
                var creditCard = await _creditCardRepository.GetByCardNumberAsync(input.CardNumber);
                var debitCard = await _debitCardRepository.GetByCardNumberAsync(input.CardNumber);

                if (creditCard == null && debitCard == null)
                {
                    result.Status = "Declined";
                    result.DeclineReason = "Geçersiz Kart";
                    return result;
                }

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

                if (isBlocked)
                {
                    result.Status = "Declined";
                    result.DeclineReason = "Kart Blokeli";
                }
                else if (cardCvv != input.CVV) 
                {
                    result.Status = "Declined";
                    result.DeclineReason = "Hatalı CVV";
                }
                else
                {
                    result.Status = "Approved";
                }

                decimal processedAmount = await _currencyService.ConvertToTryAsync(input.Amount, input.Currency);

                bool isSuspicious = false;
                string? triggeredRuleCode = null;
                string? capturedFraudReason = null;


                if ((int)input.TransactionType == 2 || (int)input.TransactionType == 3) 
                {
                    if (input.OriginalTransactionId == null)
                    {
                        result.Status = "Declined";
                        result.DeclineReason = "Orijinal işlem ID'si belirtilmelidir.";
                        return result;
                    }

                    var originalTx = await _transactionRepository.GetByIdAsync(input.OriginalTransactionId.Value);
                    if (originalTx == null)
                    {
                        result.Status = "Declined";
                        result.DeclineReason = "Orijinal işlem bulunamadı.";
                        return result;
                    }
                    if (originalTx.TransactionTypeId != 1 || originalTx.Status != "Approved")
                    {
                        result.Status = "Declined";
                        result.DeclineReason = "Referans gösterilen işlem onaylanmış bir satış işlemi değildir.";
                        return result;
                    }
                    if ((isCredit && originalTx.CreditCardId != cardId) || (!isCredit && originalTx.DebitCardId != cardId))
                    {
                        result.Status = "Declined";
                        result.DeclineReason = "İşlem yapılan kart, orijinal işlemin kartı ile eşleşmiyor.";
                        return result;
                    }

                    if ((int)input.TransactionType == 3) // Void
                    {
                        if (originalTx.Amount != input.Amount || originalTx.Currency != input.Currency)
                        {
                            result.Status = "Declined";
                            result.DeclineReason = "İptal tutarı veya para birimi orijinal işlem ile eşleşmiyor.";
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

                        var evaluationResult = await _fraudEvaluationService.EvaluateAsync(input, cardId);
                        triggeredRuleCode = evaluationResult.RuleCode;
                        capturedFraudReason = evaluationResult.FraudReason;
                        
                        isSuspicious = !string.IsNullOrEmpty(triggeredRuleCode);
                        if (isSuspicious)
                        {
                            originalTx.Status = "SuspiciousVoid";
                            await _unitOfWork.SaveChangesAsync();

                            await _fraudEvaluationService.CreateFraudLogAsync(originalTx.TransactionId, triggeredRuleCode);
                            await _unitOfWork.SaveChangesAsync();

                            result.Status = "Suspicious";
                            result.DeclineReason = $"Fraud Şüphesi: {triggeredRuleCode}";
                            result.TransactionId = originalTx.TransactionId;
                            return result;
                        }
                        else
                        {
                            originalTx.Status = "Void";
                            originalTx.RefundTime = DateTime.Now;
                            await _unitOfWork.SaveChangesAsync();

                            result.Status = "Approved";
                            result.TransactionId = originalTx.TransactionId;
                            return result;
                        }
                    }
                    else // Refund
                    {
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

                        var evaluationResult = await _fraudEvaluationService.EvaluateAsync(input, cardId);
                        triggeredRuleCode = evaluationResult.RuleCode;
                        capturedFraudReason = evaluationResult.FraudReason;
                        
                        isSuspicious = !string.IsNullOrEmpty(triggeredRuleCode);
                        if (isSuspicious)
                        {
                            originalTx.Status = "SuspiciousRefund";
                            await _unitOfWork.SaveChangesAsync();

                            await _fraudEvaluationService.CreateFraudLogAsync(originalTx.TransactionId, triggeredRuleCode);
                            await _unitOfWork.SaveChangesAsync();

                            result.Status = "Suspicious";
                            result.DeclineReason = $"Fraud Şüphesi: {triggeredRuleCode}";
                            result.TransactionId = originalTx.TransactionId;
                            return result;
                        }
                        else
                        {
                            originalTx.Status = "Refund";
                            originalTx.RefundTime = DateTime.Now;
                            await _unitOfWork.SaveChangesAsync();

                            result.Status = "Approved";
                            result.TransactionId = originalTx.TransactionId;
                            return result;
                        }
                    }
                }
                else if ((int)input.TransactionType == 1)
                {
                    bool isInitialDecline = result.Status == "Declined";

                    decimal availableFunds = isCredit ? creditCard.AvailableLimit : debitCard.Balance;
                    if (!isInitialDecline && availableFunds < processedAmount)
                    {
                        result.Status = "Declined";
                        result.DeclineReason = "Yetersiz Bakiye";
                        isInitialDecline = true;
                    }

                    if (!isBlocked)
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


                var newTransaction = new ETransaction
                {
                    CreditCardId = isCredit ? cardId : null,
                    DebitCardId = isCredit ? null : cardId,
                    TransactionTypeId = (int)input.TransactionType,
                    PaymentType = input.PaymentType,
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
                    OriginalTransactionId = input.OriginalTransactionId
                };

                await _transactionRepository.AddAsync(newTransaction);
                
                if (isCredit)
                {
                    await _creditCardRepository.UpdateAsync(creditCard);
                }
                else
                {
                    await _debitCardRepository.UpdateAsync(debitCard);
                }
                
                await _unitOfWork.SaveChangesAsync();

                if (isSuspicious && triggeredRuleCode != null)
                {
                    await _fraudEvaluationService.CreateFraudLogAsync(newTransaction.TransactionId, triggeredRuleCode);
                    await _unitOfWork.SaveChangesAsync(); 
                }

                result.TransactionId = newTransaction.TransactionId;
                return result;
            }
        }
    }
}

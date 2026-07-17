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
        private readonly IUnitOfWork _unitOfWork;

        public TransactionService(
            ICreditCardRepository creditCardRepository,
            IDebitCardRepository debitCardRepository,
            ITransactionRepository transactionRepository,
            IFraudEvaluationService fraudEvaluationService,
            IUnitOfWork unitOfWork)
        {
            _creditCardRepository = creditCardRepository;
            _debitCardRepository = debitCardRepository;
            _transactionRepository = transactionRepository;
            _fraudEvaluationService = fraudEvaluationService;
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

                if (senderDebit.Balance < input.Amount)
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

                var evaluationResult = await _fraudEvaluationService.EvaluateAsync(input, senderDebit.CardId);
                bool isSuspicious = !string.IsNullOrEmpty(evaluationResult.RuleCode);

                if (isSuspicious)
                {
                    result.Status = "Suspicious";
                    result.DeclineReason = $"Fraud Şüphesi: {evaluationResult.RuleCode}";
                }
                else
                {
                    senderDebit.Balance -= input.Amount;
                    if (receiverDebit != null)
                    {
                        receiverDebit.Balance += input.Amount;
                        await _debitCardRepository.UpdateAsync(receiverDebit);
                    }
                    await _debitCardRepository.UpdateAsync(senderDebit);
                    result.Status = "Approved";
                }

                
                var transferTx = new ETransaction
                {
                    DebitCardId = senderDebit.CardId,
                    SenderIBAN = input.SenderIBAN,
                    ReceiverIBAN = input.ReceiverIBAN,
                    ReceiverName = input.ReceiverName,
                    Description = input.Description,
                    TransactionTypeId = 4,
                    PaymentTypeId = (int)input.PaymentType,
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

                decimal processedAmount = input.Amount;
                if (input.Currency == "USD") processedAmount = input.Amount * 40; 
                else if (input.Currency == "EUR") processedAmount = input.Amount * 43;

                bool isSuspicious = false;
                string? triggeredRuleCode = null;
                string? capturedFraudReason = null;


                if ((int)input.TransactionType == 2) 
                {
                    int unrefundedSalesCount = await _transactionRepository.GetUnrefundedSaleCountAsync(cardId, input.Amount, input.Currency);
                    if (unrefundedSalesCount <= 0)
                    {
                        result.Status = "Declined";
                        result.DeclineReason = "Belirtilen İade sebebiyle eşleşen satış bulunmamaktadır.";
                    }
                    else
                    {
                        if (isCredit)
                        {
                            creditCard.AvailableLimit = Math.Min(creditCard.AvailableLimit + processedAmount, creditCard.CardLimit);
                        }
                        else
                        {
                            debitCard.Balance += processedAmount;
                        }

                        var evaluationResult = await _fraudEvaluationService.EvaluateAsync(input, cardId);
                        triggeredRuleCode = evaluationResult.RuleCode;
                        capturedFraudReason = evaluationResult.FraudReason;
                        
                        isSuspicious = !string.IsNullOrEmpty(triggeredRuleCode);
                        if (isSuspicious)
                        {
                            result.Status = "Suspicious";
                            result.DeclineReason = $"Fraud Şüphesi: {triggeredRuleCode}";
                        }
                        else
                        {
                            result.Status = "Approved";
                        }
                    }
                }
                else if ((int)input.TransactionType == 3)
                {
                    result.Status = "Approved";
                    if (isCredit)
                    {
                        creditCard.AvailableLimit += processedAmount;
                    }
                    else
                    {
                        debitCard.Balance += processedAmount;
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
                        if (isSuspicious)
                        {
                            result.Status = "Suspicious";
                            result.DeclineReason = $"Fraud Şüphesi: {triggeredRuleCode}";
                        }
                        else if (isInitialDecline)
                        {
                            result.Status = "Declined";
                        }
                        else
                        {
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
                    PaymentTypeId = (int)input.PaymentType,
                    ChannelTypeId = input.ChannelTypeId == 0 ? 2 : input.ChannelTypeId,
                    Amount = input.Amount,
                    Currency = input.Currency,
                    TransactionDate = DateTime.Now,
                    Location = input.Location,
                    Country = input.Country,
                    MerchantCategory = input.MerchantCategory,
                    Status = result.Status,
                    DeclineReason = result.Status == "Suspicious" ? $"Fraud: {triggeredRuleCode}" : result.DeclineReason,
                    FraudReason = capturedFraudReason 
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

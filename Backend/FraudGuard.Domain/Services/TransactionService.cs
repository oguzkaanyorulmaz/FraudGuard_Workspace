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
        private readonly ITransactionRepository _transactionRepository;
        private readonly IFraudEvaluationService _fraudEvaluationService;
        private readonly IUnitOfWork _unitOfWork;

        public TransactionService(
            ICreditCardRepository creditCardRepository,
            ITransactionRepository transactionRepository,
            IFraudEvaluationService fraudEvaluationService,
            IUnitOfWork unitOfWork)
        {
            _creditCardRepository = creditCardRepository;
            _transactionRepository = transactionRepository;
            _fraudEvaluationService = fraudEvaluationService;
            _unitOfWork = unitOfWork;
        }

        public async Task<TransactionCheckResult> ProcessTransactionAsync(ProcessTransactionInput input)
        {
            var result = new TransactionCheckResult();

            var card = await _creditCardRepository.GetByCardNumberAsync(input.CardNumber);
            if (card == null)
            {
                result.Status = "Declined";
                result.DeclineReason = "Geçersiz Kart";
                return result;
            }

            if (card.IsBlocked)
            {
                result.Status = "Declined";
                result.DeclineReason = "Kart Blokeli";
            }
            else if (card.CVV != input.CVV) 
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

            if (input.TransactionType == TransactionTypeEnum.Refund || input.TransactionType == TransactionTypeEnum.Void)
            {
                result.Status = "Approved";
                card.CardLimit += processedAmount;
            }
            else if (input.TransactionType == TransactionTypeEnum.Sale)
            {
                if (result.Status == "Approved" && card.CardLimit < processedAmount)
                {
                    result.Status = "Declined";
                    result.DeclineReason = "Yetersiz Bakiye";
                }

                var evaluationResult = await _fraudEvaluationService.EvaluateAsync(input, card.CardId);
                
                triggeredRuleCode = evaluationResult.RuleCode;
                capturedFraudReason = evaluationResult.FraudReason;
                
                isSuspicious = !string.IsNullOrEmpty(triggeredRuleCode);

                if (isSuspicious)
                {
                    result.Status = "Suspicious";
                }
                else if (result.Status == "Approved")
                {
                    card.CardLimit -= processedAmount;
                }
            }

            var newTransaction = new ETransaction
            {
                CardId = card.CardId,
                TransactionTypeId = (int)input.TransactionType,
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
            await _creditCardRepository.UpdateAsync(card);
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
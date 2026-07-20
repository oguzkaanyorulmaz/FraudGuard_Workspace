using FraudGuard.Domain.Entities;
using FraudGuard.Domain.Interfaces.Abstractions;
using FraudGuard.Domain.Interfaces.DomainServices;
using FraudGuard.Domain.Interfaces.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FraudGuard.Domain.Services
{
    public class AdminOperationService : IAdminOperationService
    {
        private readonly IFraudLogRepository _fraudLogRepository;
        private readonly ICreditCardRepository _creditCardRepository;
        private readonly IDebitCardRepository _debitCardRepository;
        private readonly ICurrencyService _currencyService;
        private readonly IUnitOfWork _unitOfWork;

        public AdminOperationService(
            IFraudLogRepository fraudLogRepository,
            ICreditCardRepository creditCardRepository,
            IDebitCardRepository debitCardRepository,
            ICurrencyService currencyService,
            IUnitOfWork unitOfWork)
        {
            _fraudLogRepository = fraudLogRepository;
            _creditCardRepository = creditCardRepository;
            _debitCardRepository = debitCardRepository;
            _currencyService = currencyService;
            _unitOfWork = unitOfWork;
        }

        public async Task<List<EFraudLog>> GetUnresolvedLogsAsync()
        {
            return await _fraudLogRepository.GetUnresolvedLogsAsync();
        }

public async Task<bool> ResolveFraudLogAsync(
            int logId, 
            string adminAction, 
            string adminNote, 
            int? blockReasonId = null, 
            string? resolvedByAdmin = null)
        {
            var log = await _fraudLogRepository.GetLogWithDetailsAsync(logId);
            if (log == null) return false;
            log.IsResolved = true;
            log.AdminAction = adminAction;
            log.AdminNote = adminNote;
            log.Status = "Resolved";
            log.ResolvedByAdmin = resolvedByAdmin;
        if (log.Transaction != null)
        {
            decimal convertedAmount = await _currencyService.ConvertToTryAsync(log.Transaction.Amount, log.Transaction.Currency);

            if (adminAction == "MarkAsSafe" || adminAction == "APPROVE" || adminAction == "Approve")
            {
                if (log.Transaction.Status == "SuspiciousRefund")
                {
                    log.Transaction.Status = "Refund";
                    log.Transaction.RefundTime = DateTime.Now;
                }
                else if (log.Transaction.Status == "SuspiciousVoid")
                {
                    log.Transaction.Status = "Void";
                    log.Transaction.RefundTime = DateTime.Now;
                }
                else
                {
                    log.Transaction.Status = "Approved";
                }

                // EFT/Havale durumunda parayı alıcıya aktar
                if (log.Transaction.TransactionTypeId == 4 && !string.IsNullOrEmpty(log.Transaction.ReceiverIBAN))
                {
                    var receiverDebit = await _debitCardRepository.GetByIBANAsync(log.Transaction.ReceiverIBAN);
                    if (receiverDebit != null)
                    {
                        receiverDebit.Balance += convertedAmount;
                        await _debitCardRepository.UpdateAsync(receiverDebit);
                    }
                }
            }
            else if (adminAction == "CardBlocked" || adminAction == "MarkAsFraud" || adminAction == "BLOCK")
            {
                bool isRefundOrVoidReversal = log.Transaction.Status == "SuspiciousRefund" || log.Transaction.Status == "SuspiciousVoid";

                if (isRefundOrVoidReversal)
                {
                    log.Transaction.Status = "Approved";
                    log.Transaction.RefundTime = null;
                }
                else
                {
                    log.Transaction.Status = "Declined";
                }

                if (log.Transaction.CreditCardId.HasValue)
                {
                    var creditCard = await _creditCardRepository.GetByIdAsync(log.Transaction.CreditCardId.Value);
                    if (creditCard != null)
                    {
                        if (isRefundOrVoidReversal)
                        {
                            creditCard.AvailableLimit = Math.Max(0, creditCard.AvailableLimit - convertedAmount);
                        }
                        else
                        {
                            creditCard.AvailableLimit = Math.Min(creditCard.AvailableLimit + convertedAmount, creditCard.CardLimit);
                        }
                        creditCard.IsBlocked = true;
                        creditCard.BlockReasonId = blockReasonId;
                        await _creditCardRepository.UpdateAsync(creditCard);
                    }
                }
                else if (log.Transaction.DebitCardId.HasValue)
                {
                    var debitCard = await _debitCardRepository.GetByIdAsync(log.Transaction.DebitCardId.Value);
                    if (debitCard != null)
                    {
                        if (isRefundOrVoidReversal)
                        {
                            debitCard.Balance = Math.Max(0, debitCard.Balance - convertedAmount);
                        }
                        else
                        {
                            debitCard.Balance += convertedAmount;
                        }
                        debitCard.IsBlocked = true;
                        debitCard.BlockReasonId = blockReasonId;
                        await _debitCardRepository.UpdateAsync(debitCard);
                    }
                }
            }
        }

        await _fraudLogRepository.UpdateAsync(log);
        await _unitOfWork.SaveChangesAsync();
        return true;
}
    }
}
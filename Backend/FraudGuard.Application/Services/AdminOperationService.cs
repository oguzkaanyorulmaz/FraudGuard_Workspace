using FraudGuard.Domain.Entities;
using FraudGuard.Domain.Interfaces.Abstractions;
using FraudGuard.Application.Interfaces;
using FraudGuard.Domain.Interfaces.DomainServices;
using FraudGuard.Domain.Interfaces.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FraudGuard.Application.Services
{
    public class AdminOperationService : IAdminOperationService
    {
        private readonly IFraudLogRepository _fraudLogRepository;
        private readonly ICreditCardRepository _creditCardRepository;
        private readonly IDebitCardRepository _debitCardRepository;
        private readonly ICurrencyService _currencyService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheProvider _cacheProvider;
        private readonly IBankAccountBeneficiaryRepository _bankAccountBeneficiaryRepository;

        public AdminOperationService(
            IFraudLogRepository fraudLogRepository,
            ICreditCardRepository creditCardRepository,
            IDebitCardRepository debitCardRepository,
            ICurrencyService currencyService,
            IUnitOfWork unitOfWork,
            ICacheProvider cacheProvider,
            IBankAccountBeneficiaryRepository bankAccountBeneficiaryRepository)
        {
            _fraudLogRepository = fraudLogRepository;
            _creditCardRepository = creditCardRepository;
            _debitCardRepository = debitCardRepository;
            _currencyService = currencyService;
            _unitOfWork = unitOfWork;
            _cacheProvider = cacheProvider;
            _bankAccountBeneficiaryRepository = bankAccountBeneficiaryRepository;
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

            if (adminAction == "MarkAsSafe" || adminAction == "APPROVE" || adminAction == "Approve" || adminAction == "APPROVED")
            {
                log.Transaction.Status = "Approved";
                log.Transaction.DeclineReason = null;

                if (log.Transaction.TransactionTypeId == 4 && !string.IsNullOrEmpty(log.Transaction.ReceiverIBAN))
                {
                    var receiverDebit = await _debitCardRepository.GetByIBANAsync(log.Transaction.ReceiverIBAN);
                    if (receiverDebit != null)
                    {
                        receiverDebit.Balance += convertedAmount;
                        await _debitCardRepository.UpdateAsync(receiverDebit);
                    }

                    if (!string.IsNullOrEmpty(log.Transaction.SenderIBAN))
                    {
                        var senderDebit = await _debitCardRepository.GetByIBANAsync(log.Transaction.SenderIBAN);
                        if (senderDebit != null)
                        {
                            bool hasBeneficiary = await _bankAccountBeneficiaryRepository.AnyAsync(senderDebit.CustomerId, log.Transaction.ReceiverIBAN);
                            if (!hasBeneficiary)
                            {
                                var beneficiary = new EBankAccountBeneficiary
                                {
                                    CustomerId = senderDebit.CustomerId,
                                    ReceiverIBAN = log.Transaction.ReceiverIBAN,
                                    ReceiverName = log.Transaction.ReceiverName ?? "Alıcı",
                                    AddedDate = System.DateTime.Now
                                };
                                await _bankAccountBeneficiaryRepository.AddAsync(beneficiary);
                            }
                        }
                    }
                }
            }
            else if (adminAction == "CardBlocked" || adminAction == "MarkAsFraud" || adminAction == "BLOCK")
            {
                bool isRefundReversal = log.Transaction.Status == "SuspiciousRefund";

                log.Transaction.Status = "Declined";

                if (log.Transaction.CreditCardId.HasValue)
                {
                    var creditCard = await _creditCardRepository.GetByIdAsync(log.Transaction.CreditCardId.Value);
                    if (creditCard != null)
                    {
                        if (isRefundReversal)
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
                        if (isRefundReversal)
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
                else if (log.Transaction.TransactionTypeId == 4 && !string.IsNullOrEmpty(log.Transaction.SenderIBAN))
                {
                    var debitCard = await _debitCardRepository.GetByIBANAsync(log.Transaction.SenderIBAN);
                    if (debitCard != null)
                    {
                        if (isRefundReversal)
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

        if (log.Transaction != null)
        {
            if (log.Transaction is ECreditCardTransaction ccTx && ccTx.CreditCard != null)
            {
                await _cacheProvider.RemoveAsync($"card_info_{ccTx.CreditCard.CardNumber}");
            }
            if (log.Transaction is EDebitCardTransaction dcTx && dcTx.DebitCard != null)
            {
                await _cacheProvider.RemoveAsync($"card_info_{dcTx.DebitCard.CardNumber}");
            }
            if (log.Transaction.TransactionTypeId == 4 && !string.IsNullOrEmpty(log.Transaction.SenderIBAN))
            {
                var senderDebit = await _debitCardRepository.GetByIBANAsync(log.Transaction.SenderIBAN);
                if (senderDebit != null)
                {
                    await _cacheProvider.RemoveAsync($"card_info_{senderDebit.CardNumber}");
                }
            }
            if (log.Transaction.TransactionTypeId == 4 && !string.IsNullOrEmpty(log.Transaction.ReceiverIBAN))
            {
                var receiverDebit = await _debitCardRepository.GetByIBANAsync(log.Transaction.ReceiverIBAN);
                if (receiverDebit != null)
                {
                    await _cacheProvider.RemoveAsync($"card_info_{receiverDebit.CardNumber}");
                }
            }
        }

        return true;
}
    }
}
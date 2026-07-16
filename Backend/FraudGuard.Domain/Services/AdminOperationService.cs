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
        private readonly IUnitOfWork _unitOfWork;

        public AdminOperationService(
            IFraudLogRepository fraudLogRepository,
            ICreditCardRepository creditCardRepository,
            IUnitOfWork unitOfWork)
        {
            _fraudLogRepository = fraudLogRepository;
            _creditCardRepository = creditCardRepository;
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
        if (adminAction == "MarkAsSafe" || adminAction == "APPROVE" || adminAction == "Approve")
        {
            log.Transaction.Status = "Approved";

            if (log.Transaction.CreditCard != null)
            {
                decimal processedAmount = log.Transaction.Amount;
                
                if (log.Transaction.Currency == "USD") 
                    processedAmount = log.Transaction.Amount * 40;
                else if (log.Transaction.Currency == "EUR") 
                    processedAmount = log.Transaction.Amount * 43;

                log.Transaction.CreditCard.AvailableLimit -= processedAmount;
                
                await _creditCardRepository.UpdateAsync(log.Transaction.CreditCard);
            }
        }
        else if (adminAction == "CardBlocked" || adminAction == "MarkAsFraud" || adminAction == "BLOCK")
        {
            log.Transaction.Status = "Declined";
        }
    }


    await _fraudLogRepository.UpdateAsync(log);

    if ((adminAction == "CardBlocked" || adminAction == "MarkAsFraud" || adminAction == "BLOCK") && log.TransactionId > 0)
    {
        if (log.Transaction != null)
        {
            var card = await _creditCardRepository.GetByIdAsync(log.Transaction.CardId);
            if (card != null)
            {
                card.IsBlocked = true;
                card.BlockReasonId = blockReasonId; 
                await _creditCardRepository.UpdateAsync(card);
            }
        }
    }

    await _unitOfWork.SaveChangesAsync();
    return true;
}
    }
}
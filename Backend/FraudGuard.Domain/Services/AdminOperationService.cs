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

public async Task<bool> ResolveFraudLogAsync(int logId, string adminAction, string adminNote, int? blockReasonId = null)
{
    var log = await _fraudLogRepository.GetLogWithDetailsAsync(logId);
    if (log == null) return false;

    log.IsResolved = true;
    log.AdminAction = adminAction;
    log.AdminNote = adminNote;
    log.Status = "Resolved";

    if (log.Transaction != null)
    {
        if (adminAction == "MarkAsSafe" || adminAction == "APPROVE" || adminAction == "Approve")
            log.Transaction.Status = "Approved";
        else if (adminAction == "CardBlocked" || adminAction == "MarkAsFraud" || adminAction == "BLOCK")
            log.Transaction.Status = "Declined";
    }

    await _fraudLogRepository.UpdateAsync(log);

    if ((adminAction == "CardBlocked" || adminAction == "MarkAsFraud" || adminAction == "BLOCK") && log.TransactionId > 0)
    {
        if(log.Transaction != null && blockReasonId.HasValue)
        {
            var card = await _creditCardRepository.GetByIdAsync(log.Transaction.CardId);
            if (card != null)
            {
                card.IsBlocked = true;
                card.BlockReasonId = blockReasonId.Value; 
                await _creditCardRepository.UpdateAsync(card);
            }
        }
    }

    await _unitOfWork.SaveChangesAsync();
    return true;
}
    }
}
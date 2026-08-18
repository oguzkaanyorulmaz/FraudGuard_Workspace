using FraudGuard.Domain.Common.Enums;
using FraudGuard.Domain.DomainObjects.FraudEvaluation;
using FraudGuard.Domain.DomainObjects.TransactionProcessing;
using System.Threading.Tasks;

namespace FraudGuard.Application.Interfaces
{
    public interface IFraudEvaluationService
    {
        /// <summary>
        /// Tüm aktif kuralları çalıştırır, puanları biriktirir ve 4 kademeli nihai kararı üretir.
        /// İlk eşleşmede durmaz.
        /// </summary>
        /// <param name="cardId">Güven skoru geçmişinin sorgulanacağı kart. Bilinmiyorsa 0.</param>
        /// <param name="isCreditCard">Kart tipi. Alarm geçmişi sorgusunun hedefini belirler.</param>
        Task<FraudDecisionResult> EvaluateAsync(ProcessTransactionInput input, int cardId, bool isCreditCard);

        /// <summary>
        /// Tetiklenen birincil kural için fraud log kaydı açar.
        /// Tam kural kırılımı işlemin <c>FraudReason</c> alanına yazılır.
        /// </summary>
        Task CreateFraudLogAsync(
            int transactionId, 
            string ruleCode, 
            PaymentTypeEnum paymentType,
            bool isAutoBlocked = false,
            string? resolvedBy = null,
            string? adminNote = null);
    }
}

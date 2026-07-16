using FraudGuard.Domain.DomainObjects.TransactionProcessing;
using FraudGuard.Domain.Entities;
using FraudGuard.Domain.Interfaces.DomainServices;
using FraudGuard.Domain.Interfaces.Repositories;
using FraudGuard.Domain.Common.Enums;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FraudGuard.Domain.Services
{
    public class FraudEvaluationService : IFraudEvaluationService
    {
        private readonly ITransactionRepository _transactionRepository;
        private readonly ICreditCardRepository _creditCardRepository;
        private readonly IFraudRuleRepository _fraudRuleRepository;
        private readonly IFraudLogRepository _fraudLogRepository;

        public FraudEvaluationService(
            ITransactionRepository transactionRepository,
            ICreditCardRepository creditCardRepository,
            IFraudRuleRepository fraudRuleRepository,
            IFraudLogRepository fraudLogRepository)
        {
            _transactionRepository = transactionRepository;
            _creditCardRepository = creditCardRepository;
            _fraudRuleRepository = fraudRuleRepository;
            _fraudLogRepository = fraudLogRepository;
        }

        public async Task<(string? RuleCode, string? FraudReason)> EvaluateAsync(ProcessTransactionInput input, int cardId)
        {
            var recentTransactions = await _transactionRepository.GetRecentTransactionsAsync(cardId, TimeSpan.FromHours(24));
            var card = await _creditCardRepository.GetByIdAsync(cardId);

            // ==========================================
            // SENARYO 10: Ardışık İade Kuralı (Consecutive Refunds)
            // ==========================================
            if (input.TransactionType == TransactionTypeEnum.Refund)
            {
                var recentRefundsOrdered = recentTransactions
                    .OrderByDescending(t => t.TransactionDate)
                    .ToList();

                int consecutiveRefunds = 1;
                foreach (var tx in recentRefundsOrdered)
                {
                    if (tx.TransactionTypeId == 2)
                    {
                        consecutiveRefunds++;
                    }
                    else
                    {
                        break;
                    }
                }

                if (consecutiveRefunds >= 3)
                {
                    return ("CONSECUTIVE_REFUNDS", $"Son 24 saat içinde ardışık {consecutiveRefunds} iade (Refund) işlemi denemesi yapıldı.");
                }
            }

            if ((int)input.TransactionType == 2 || (int)input.TransactionType == 3)
            {
                return (null, null);
            }

            var lastApprovedTx = recentTransactions.OrderByDescending(t => t.TransactionDate).FirstOrDefault(t => t.Status == "Approved");
            if (lastApprovedTx != null && lastApprovedTx.Location != input.Location)
            {
                if ((DateTime.Now - lastApprovedTx.TransactionDate).TotalMinutes <= 10)
                    return ("IMPOSSIBLE_TRAVEL", $"10 dakika içinde önce {lastApprovedTx.Location}, ardından {input.Location} lokasyonlarından işlem denemesi.");
            }
            bool isCurrentDeclined = false;
            if (card != null)
            {
                if (card.CVV != input.CVV)
                    isCurrentDeclined = true;
                decimal processedAmount = input.Amount;
                if (input.Currency == "USD") processedAmount = input.Amount * 40;
                else if (input.Currency == "EUR") processedAmount = input.Amount * 43;
                if (card.AvailableLimit < processedAmount)
                    isCurrentDeclined = true;
            }
            var recentOrdered = recentTransactions
                .Where(t => (DateTime.Now - t.TransactionDate).TotalMinutes <= 30)
                .OrderByDescending(t => t.TransactionDate)
                .ToList();
            int consecutiveDeclines = 0;
            foreach (var tx in recentOrdered)
            {
                if (tx.Status == "Declined")
                {
                    if (tx.DeclineReason == "Hatalı CVV" || tx.DeclineReason == "Yetersiz Bakiye")
                    {
                        consecutiveDeclines++;
                    }
                }
                else if (tx.Status == "Approved")
                {
                    break;
                }
            }
            if ((isCurrentDeclined && consecutiveDeclines >= 2) || (!isCurrentDeclined && consecutiveDeclines >= 3))
            {
                int totalDeclines = isCurrentDeclined ? consecutiveDeclines + 1 : consecutiveDeclines;
                return ("BRUTE_FORCE", $"Son 30 dakika içerisinde ardışık {totalDeclines} veya daha fazla reddedilmiş işlem denemesi yapıldı.");
            }
            var smallTestTx = recentTransactions.FirstOrDefault(t => t.Amount <= 10 && (DateTime.Now - t.TransactionDate).TotalMinutes <= 30);
            if (smallTestTx != null && input.Amount >= 20000)
                return ("CARD_TESTING", "Küçük tutarlı (yoklama) bir işlemin hemen ardından yüklü miktarda çekim denemesi yapıldı.");
            if (card != null && card.CardLimit > 0)
            {
                decimal limitUsagePercentage = (input.Amount / card.CardLimit) * 100;
                if (limitUsagePercentage >= 95)
                    return ("MAX_OUT", "Tek seferde kart limitinin %95'ini veya daha fazlasını boşaltma denemesi.");
            }
            int currentHour = DateTime.Now.Hour;
            if (currentHour >= 2 && currentHour <= 5 && input.Amount >= 100000)
                return ("ANOMALOUS_TIME", "Gece 02:00 - 05:00 saatleri arasında olağandışı yüksek tutarlı (100.000+) işlem denemesi.");
            bool hasForeignTxBefore = recentTransactions.Any(t => t.Country != "Türkiye");
            if (!hasForeignTxBefore && input.Country != "Türkiye")
                return ("CROSS_BORDER", "Müşterinin geçmişinde yurt dışı işlemi bulunmamasına rağmen Türkiye dışından işlem denemesi yapıldı.");
            if (input.Currency != "TRY")
            {
                bool hasUsedCurrencyBefore = recentTransactions.Any(t => t.Currency == input.Currency && t.Status == "Approved");
                if (!hasUsedCurrencyBefore)
                {
                    return ("CURRENCY_MISMATCH", $"Müşteri geçmişinde daha önce hiç {input.Currency} para birimiyle işlem kaydı bulunmuyor."); 
                }
            }
            string[] highRiskMcc = { "Kuyumcu", "Bahis", "Kripto Borsası" };
            if (highRiskMcc.Contains(input.MerchantCategory))
            {
                if (input.Amount > 10000) 
                    return ("HIGH_RISK_MCC", "Yüksek riskli işyeri kategorisinden (Kuyumcu, Bahis vb.) yüksek tutarlı çekim denemesi.");
            }
            var countInLast5Mins = recentTransactions.Count(t => 
                (DateTime.Now - t.TransactionDate).TotalMinutes <= 5 && 
                t.Status == "Approved" &&
                t.TransactionTypeId == 1);
            if (countInLast5Mins >= 3) 
                return ("VELOCITY", "Son 5 dakika içerisinde 3'ten fazla satış işlemi denemesi tespit edildi.");
            return (null, null); 
        }

        public async Task CreateFraudLogAsync(int transactionId, string ruleCode)
        {
            var rule = await _fraudRuleRepository.GetByCodeAsync(ruleCode);
            if (rule != null && rule.IsActive)
            {
                var log = new EFraudLog
                {
                    TransactionId = transactionId,
                    RuleId = rule.RuleId,
                    LogDate = DateTime.Now,
                    Status = "Unresolved" 
                };
                await _fraudLogRepository.AddAsync(log);
            }
        }
    }
}
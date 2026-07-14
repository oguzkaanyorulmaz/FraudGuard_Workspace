using FraudGuard.Domain.DomainObjects.TransactionProcessing;
using FraudGuard.Domain.Entities;
using FraudGuard.Domain.Interfaces.DomainServices;
using FraudGuard.Domain.Interfaces.Repositories;
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
            // ==========================================
            // GÜVENLİK DUVARI: İade/İptal İşlemlerini Reddet
            // ==========================================
            if ((int)input.TransactionType == 2 || (int)input.TransactionType == 3)
            {
                return (null, null); // İade/İptal işlemleri risk taşımaz
            }

            var recentTransactions = await _transactionRepository.GetRecentTransactionsAsync(cardId, TimeSpan.FromHours(24));
            var card = await _creditCardRepository.GetByIdAsync(cardId);

            // ==========================================
            // SENARYO 1: Hız/Sıklık Kuralı (Velocity)
            // ==========================================
            var countInLast5Mins = recentTransactions.Count(t => 
            (DateTime.Now - t.TransactionDate).TotalMinutes <= 5 && 
             t.Status == "Approved"); 

            if (countInLast5Mins >= 2) 
             return ("VELOCITY", "Son 5 dakika içerisinde 2'den fazla işlem denemesi tespit edildi.");

            // ==========================================
            // SENARYO 2: Lokasyon Kuralı (Impossible Travel)
            // ==========================================
            var lastApprovedTx = recentTransactions.OrderByDescending(t => t.TransactionDate).FirstOrDefault(t => t.Status == "Approved");
            if (lastApprovedTx != null && lastApprovedTx.Location != input.Location)
            {
                if ((DateTime.Now - lastApprovedTx.TransactionDate).TotalMinutes <= 10)
                    return ("IMPOSSIBLE_TRAVEL", $"10 dakika içinde önce {lastApprovedTx.Location}, ardından {input.Location} lokasyonlarından işlem denemesi.");
            }

            // ==========================================
            // SENARYO 3: Zaman ve Tutar Kuralı (Anomalous Time)
            // ==========================================
            int currentHour = DateTime.Now.Hour;
            if (currentHour >= 2 && currentHour <= 5 && input.Amount >= 100000)
                return ("ANOMALOUS_TIME", "Gece 02:00 - 05:00 saatleri arasında olağandışı yüksek tutarlı (100.000+) işlem denemesi.");

            // ==========================================
            // SENARYO 4: Yoklama Çekimi (Card Testing)
            // ==========================================
            var smallTestTx = recentTransactions.FirstOrDefault(t => t.Amount <= 10 && (DateTime.Now - t.TransactionDate).TotalMinutes <= 30);
            if (smallTestTx != null && input.Amount >= 20000)
                return ("CARD_TESTING", "Küçük tutarlı (yoklama) bir işlemin hemen ardından yüklü miktarda çekim denemesi yapıldı.");

            // ==========================================
            // SENARYO 5: Ardışık Hata (Brute Force)
            // ==========================================
            var recentDeclines = recentTransactions
                .Where(t => t.Status == "Declined" && (DateTime.Now - t.TransactionDate).TotalMinutes <= 30)
                .OrderByDescending(t => t.TransactionDate)
                .Take(3)
                .Count();
            if (recentDeclines >= 3)
                return ("BRUTE_FORCE", "Son 30 dakika içerisinde ardışık 3 veya daha fazla reddedilmiş işlem denemesi yapıldı.");

            // ==========================================
            // SENARYO 6: Sınır Ötesi İşlem (Cross Border)
            // ==========================================
            bool hasForeignTxBefore = recentTransactions.Any(t => t.Country != "Türkiye");
            if (!hasForeignTxBefore && input.Country != "Türkiye")
                return ("CROSS_BORDER", "Müşterinin geçmişinde yurt dışı işlemi bulunmamasına rağmen Türkiye dışından işlem denemesi yapıldı.");

            // ==========================================
            // SENARYO 7: Yüksek Riskli İşyeri Tipi (High-Risk MCC)
            // ==========================================
            string[] highRiskMcc = { "Kuyumcu", "Bahis", "Kripto Borsası" };
            if (highRiskMcc.Contains(input.MerchantCategory))
            {
                if(input.Amount > 10000) 
                    return ("HIGH_RISK_MCC", "Yüksek riskli işyeri kategorisinden (Kuyumcu, Bahis vb.) yüksek tutarlı çekim denemesi.");
            }

            // ==========================================
            // SENARYO 8: Limit Boşaltma Denemesi (Max-Out Attempt)
            // ==========================================
            if (card != null && card.CardLimit > 0)
            {
                // Max-Out kuralı, kartın toplam limitine (CardLimit) oranlanmalıdır
                decimal limitUsagePercentage = (input.Amount / card.CardLimit) * 100;
                if (limitUsagePercentage >= 95)
                    return ("MAX_OUT", "Tek seferde kart limitinin %95'ini veya daha fazlasını boşaltma denemesi.");
            }

            // ==========================================
            // SENARYO 9: Para Birimi Anormalliği (Currency Mismatch)
            // ==========================================
            if (input.Currency != "TRY")
            {
                bool hasUsedCurrencyBefore = recentTransactions.Any(t => t.Currency == input.Currency && t.Status == "Approved");

                if (!hasUsedCurrencyBefore)
                {
                    return ("CURRENCY_MISMATCH", $"Müşteri geçmişinde daha önce hiç {input.Currency} para birimiyle işlem kaydı bulunmuyor."); 
                }
            }

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
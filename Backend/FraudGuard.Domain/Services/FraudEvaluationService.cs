using FraudGuard.Domain.DomainObjects.TransactionProcessing;
using FraudGuard.Domain.Entities;
using FraudGuard.Domain.Interfaces.DomainServices;
using FraudGuard.Domain.Interfaces.Repositories;
using FraudGuard.Domain.Common.Enums;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace FraudGuard.Domain.Services
{
    public class FraudEvaluationService : IFraudEvaluationService
    {
        private readonly ITransactionRepository _transactionRepository;
        private readonly ICreditCardRepository _creditCardRepository;
        private readonly IDebitCardRepository _debitCardRepository;
        private readonly IFraudRuleRepository _fraudRuleRepository;
        private readonly IFraudLogRepository _fraudLogRepository;

        public FraudEvaluationService(
            ITransactionRepository transactionRepository,
            ICreditCardRepository creditCardRepository,
            IDebitCardRepository debitCardRepository,
            IFraudRuleRepository fraudRuleRepository,
            IFraudLogRepository fraudLogRepository)
        {
            _transactionRepository = transactionRepository;
            _creditCardRepository = creditCardRepository;
            _debitCardRepository = debitCardRepository;
            _fraudRuleRepository = fraudRuleRepository;
            _fraudLogRepository = fraudLogRepository;
        }

        public async Task<(string? RuleCode, string? FraudReason)> EvaluateAsync(ProcessTransactionInput input, int cardId)
        {
            return await EvaluateAllRulesAsync(input);
        }

        public async Task<(string? RuleCode, string? FraudReason)> EvaluateAllRulesAsync(ProcessTransactionInput input)
        {
            List<ETransaction> recentTransactions = new();
            if (!string.IsNullOrEmpty(input.CardNumber))
            {
                var cc = await _creditCardRepository.GetByCardNumberAsync(input.CardNumber);
                if (cc != null) recentTransactions = await _transactionRepository.GetRecentTransactionsAsync(cc.CardId, TimeSpan.FromHours(24));
                else
                {
                    var dc = await _debitCardRepository.GetByCardNumberAsync(input.CardNumber);
                    if (dc != null) recentTransactions = await _transactionRepository.GetRecentTransactionsAsync(dc.CardId, TimeSpan.FromHours(24));
                }
            }
            else if (!string.IsNullOrEmpty(input.SenderIBAN))
            {
                // Gönderici IBAN üzerinden son işlemleri sorgula
                var dc = await _debitCardRepository.GetByIBANAsync(input.SenderIBAN);
                if (dc != null) recentTransactions = await _transactionRepository.GetRecentTransactionsAsync(dc.CardId, TimeSpan.FromHours(24));
            }

            // =================================================================
            // SENARYO 17: İşlem Açıklamasında Şüpheli Kelimeler (EFT/Havale)
            // =================================================================
            if (!string.IsNullOrEmpty(input.Description))
            {
                string[] blacklistedWords = { "bahis", "kripto", "kumar", "yasadışı", "giftcard", "borç kapatma" };
                if (blacklistedWords.Any(word => input.Description.ToLower().Contains(word)))
                {
                    return ("SUSPICIOUS_DESCRIPTION", $"İşlem açıklamasında yasaklı kelime tespit edildi: '{input.Description}'");
                }
            }

            // =================================================================
            // SENARYO 18: Şüpheli Alıcı Hesabı / Katır Hesap (EFT/Havale)
            // =================================================================
            if (!string.IsNullOrEmpty(input.ReceiverIBAN))
            {
                string[] blacklistedIbans = { "TR99000620000000000999999", "TR88000620000000000888888" }; // Test için kara listeye alınmış IBAN'lar
                if (blacklistedIbans.Contains(input.ReceiverIBAN))
                {
                    return ("HIGH_RISK_RECEIVER", $"Alıcı hesap ({input.ReceiverIBAN}) sistemde şüpheli/katır hesap olarak işaretlenmiştir.");
                }
            }

            // =================================================================
            // SENARYO 3: Zaman ve Tutar Kuralı (Anomalous Behavior - Herkes için)
            // =================================================================
            int currentHour = DateTime.Now.Hour;
            if (currentHour >= 2 && currentHour <= 5 && input.Amount >= 100000)
            {
                return ("ANOMALOUS_TIME", "Gece 02:00 - 05:00 saatleri arasında 100.000 TL ve üzeri yüksek tutarlı harcama/transfer denemesi.");
            }

            // =================================================================
            // KART TABANLI KONTROLLER (POS, Sanal POS, ATM)
            // =================================================================
            if (input.PaymentType == PaymentTypeEnum.CreditCard || input.PaymentType == PaymentTypeEnum.DebitCard)
            {
                // SENARYO 1: Hız/Sıklık Kuralı (Velocity)
                var countInLast5Mins = recentTransactions.Count(t => 
                    t.TransactionDate <= DateTime.Now &&
                    (DateTime.Now - t.TransactionDate).TotalMinutes <= 5 && 
                    t.Status == "Approved" && 
                    t.TransactionTypeId == 1);
                if (countInLast5Mins >= 3)
                {
                    return ("VELOCITY", "Aynı kartla son 5 dakika içinde 3 veya daha fazla işlem yapıldı.");
                }

                // SENARYO 2: Lokasyon/Mesafe Kuralı (Impossible Travel) - Sadece POS ve ATM kanallarında
                if (input.ChannelTypeId == 1 || input.ChannelTypeId == 3) // POS=1, ATM=3
                {
                    var lastPhysicalTx = recentTransactions
                        .Where(t => t.TransactionDate <= DateTime.Now && (t.ChannelTypeId == 1 || t.ChannelTypeId == 3))
                        .OrderByDescending(t => t.TransactionDate)
                        .FirstOrDefault(t => t.Status == "Approved");

                    if (lastPhysicalTx != null && lastPhysicalTx.Location != input.Location)
                    {
                        var timeDiff = (DateTime.Now - lastPhysicalTx.TransactionDate).TotalMinutes;
                        if (timeDiff <= 10)
                        {
                            return ("IMPOSSIBLE_TRAVEL", $"10 dakika arayla iki farklı fiziksel lokasyonda ({lastPhysicalTx.Location} -> {input.Location}) işlem denendi.");
                        }
                    }
                }

                // SENARYO 4: Yoklama/Deneme Çekimi (Card Testing)
                var smallTestTx = recentTransactions.FirstOrDefault(t => 
                    t.TransactionDate <= DateTime.Now &&
                    t.Amount <= 10 && 
                    (DateTime.Now - t.TransactionDate).TotalMinutes <= 10);
                if (smallTestTx != null && input.Amount >= 20000)
                {
                    return ("CARD_TESTING", "10 dakika içinde yapılan mikro deneme (1-10 TL) onayından hemen sonra yüksek tutarlı harcama denemesi.");
                }

                // SENARYO 5: Ardışık Hata / Brute Force (Brute Force)
                var last30MinDeclines = recentTransactions
                    .Where(t => t.TransactionDate <= DateTime.Now && (DateTime.Now - t.TransactionDate).TotalMinutes <= 30)
                    .OrderByDescending(t => t.TransactionDate)
                    .ToList();
                int consecutiveDeclines = 0;
                foreach (var tx in last30MinDeclines)
                {
                    if (tx.Status == "Declined") consecutiveDeclines++;
                    else if (tx.Status == "Approved") break;
                }
                if (consecutiveDeclines >= 3)
                {
                    return ("BRUTE_FORCE", "Son 30 dakikada üst üste 3 işlem reddinden sonra 4. deneme yapılmaktadır.");
                }

                // SENARYO 6: Beklenmedik Sınır Ötesi İşlem (Cross-Border)
                bool hasForeignTxBefore = recentTransactions.Any(t => t.Country != "Türkiye");
                if (!hasForeignTxBefore && input.Country != "Türkiye")
                {
                    return ("CROSS_BORDER", "Müşterinin geçmişinde yurt dışı işlemi bulunmamasına rağmen aniden sınır ötesi işlem denendi.");
                }

                // SENARYO 7: Yüksek Riskli İşyeri Tipi (High-Risk MCC)
                string[] highRiskMcc = { "Kuyumcu", "Kripto Para Borsası", "Bahis Sitesi" };
                if (highRiskMcc.Contains(input.MerchantCategory) && input.Amount >= 10000)
                {
                    return ("HIGH_RISK_MCC", $"Yüksek riskli işyerinden ({input.MerchantCategory}) 10.000 TL ve üzeri harcama denemesi.");
                }

                // SENARYO 8: Limit Boşaltma Denemesi (Max-Out - Sadece Kredi Kartı)
                if (input.PaymentType == PaymentTypeEnum.CreditCard)
                {
                    var cc = await _creditCardRepository.GetByCardNumberAsync(input.CardNumber);
                    if (cc != null && cc.CardLimit > 0)
                    {
                        if (input.Amount >= cc.CardLimit * 0.95m)
                        {
                            return ("MAX_OUT", "Kredi kartı kullanılabilir limitinin %95'i tek işlemle boşaltılmaya çalışılıyor.");
                        }
                    }
                }

                // SENARYO 15: Hesap Boşaltma Denemesi (Account Drain - Banka Kartı)
                if (input.PaymentType == PaymentTypeEnum.DebitCard)
                {
                    var dc = await _debitCardRepository.GetByCardNumberAsync(input.CardNumber);
                    if (dc != null && dc.Balance > 0)
                    {
                        if (input.Amount >= dc.Balance * 0.98m)
                        {
                            return ("ACCOUNT_DRAIN", "Banka kartının bağlı olduğu mevduat hesabının %98 veya daha fazlası tek seferde çekilmek isteniyor.");
                        }
                    }
                }

                // SENARYO 9: Para Birimi Sapması (Currency Mismatch)
                if (input.Currency != "TRY")
                {
                    bool hasUsedCurrencyBefore = recentTransactions.Any(t => t.Currency == input.Currency && t.Status == "Approved");
                    if (!hasUsedCurrencyBefore)
                    {
                        return ("CURRENCY_MISMATCH", $"Geçmişte onaylanmış {input.Currency} işlemi bulunmamasına rağmen döviz cinsinden işlem denemesi.");
                    }
                }

                // SENARYO 10: Ardışık İade Kuralı (Consecutive Refunds)
                if (input.TransactionType == TransactionTypeEnum.Refund)
                {
                    int consecutiveRefunds = recentTransactions
                        .Where(t => t.TransactionDate <= DateTime.Now)
                        .OrderByDescending(t => t.TransactionDate)
                        .TakeWhile(t => t.TransactionTypeId == 2)
                        .Count();
                    if (consecutiveRefunds >= 3)
                    {
                        return ("CONSECUTIVE_REFUNDS", "Son 24 saat içinde 3 veya daha fazla ardışık iade (Refund) işlemi denendi.");
                    }
                }
            }

            // =================================================================
            // TRANSFER TABANLI KONTROLLER (EFT / HAVALE)
            // =================================================================
            if (input.PaymentType == PaymentTypeEnum.EFT || input.PaymentType == PaymentTypeEnum.BankTransfer)
            {
                // SENARYO 11: Dilimleme / Parçalayarak Transfer (Smurfing)
                var hourlyTransfers = recentTransactions
                    .Where(t => t.TransactionDate <= DateTime.Now && (DateTime.Now - t.TransactionDate).TotalHours <= 1 && (t.PaymentTypeId == 3 || t.PaymentTypeId == 4))
                    .ToList();
                decimal totalHourlyAmount = hourlyTransfers.Sum(t => t.Amount) + input.Amount;
                if (hourlyTransfers.Count >= 2 && totalHourlyAmount >= 50000 && input.Amount < 50000)
                {
                    return ("SMURFING", "Yasal bildirim limitini (50.000 TL) aşmamak amacıyla transferlerin küçük parçalara bölünmesi şüphesi.");
                }

                // SENARYO 14: Yurt Dışı Havale / EFT Anormalliği (Cross-Border Transfer)
                if (input.Country != "Türkiye" && input.Amount >= 20000)
                {
                    bool hasInternationalTransferBefore = recentTransactions.Any(t => t.Country != "Türkiye" && (t.PaymentTypeId == 3 || t.PaymentTypeId == 4));
                    if (!hasInternationalTransferBefore)
                    {
                        return ("CROSS_BORDER_TRANSFER", "Hesap geçmişinde yurt dışı transfer kaydı bulunmayan hesaptan aniden yurt dışına limit üstü transfer.");
                    }
                }

                // SENARYO 12: Wallet Cash-Out
                var hasRecentIncomingLoad = recentTransactions.Any(t => 
                    t.TransactionDate <= DateTime.Now &&
                    (DateTime.Now - t.TransactionDate).TotalMinutes <= 15 && 
                    t.Status == "Approved" && 
                    (t.TransactionTypeId == 1 || t.PaymentTypeId == 5));
                if (hasRecentIncomingLoad)
                {
                    return ("WALLET_CASHOUT", "Son 15 dakika içinde karta/hesaba bakiye yüklemesi yapılmasının ardından hemen EFT ile çıkış denemesi (Wallet Cash-Out).");
                }

                // Alıcı geçmişini alarak çoklu fonlama ve tek alıcıya çoklu gönderici kontrolü yap
                var receiverHistory = await _transactionRepository.GetRecentTransactionsByReceiverIBANAsync(input.ReceiverIBAN, TimeSpan.FromMinutes(30));
                var distinctSendersCount = receiverHistory
                    .Where(t => t.Status == "Approved" && !string.IsNullOrEmpty(t.SenderIBAN) && t.SenderIBAN != input.SenderIBAN)
                    .Select(t => t.SenderIBAN)
                    .Distinct()
                    .Count();

                // SENARYO 19: Tek Alıcıya Çoklu Kaynaktan Transfer (4 veya daha fazla benzersiz gönderici)
                if (distinctSendersCount >= 3)
                {
                    return ("MULTI_SENDER_TO_SINGLE_RECEIVER", $"Aynı alıcı hesaba ({input.ReceiverIBAN}) son 30 dakika içinde 4 veya daha fazla farklı kişiden para transferi yapılmaktadır.");
                }

                // SENARYO 13: Çoklu Kaynakla Fonlama (3 benzersiz gönderici)
                if (distinctSendersCount == 2)
                {
                    return ("MULTI_SOURCE_FUNDING", $"Alıcı hesap ({input.ReceiverIBAN}) son 30 dakika içinde 3 farklı hesaptan fonlanmıştır (Çoklu Kaynakla Fonlama).");
                }

                // SENARYO 16: Şüpheli Yeni Alıcı Transferi
                if (input.Amount >= 15000)
                {
                    bool hasPriorTransferToThisReceiver = recentTransactions.Any(t => 
                        t.ReceiverIBAN == input.ReceiverIBAN && 
                        t.Status == "Approved");
                    if (!hasPriorTransferToThisReceiver)
                    {
                        return ("NEW_BENEFICIARY_TRANSFER", $"Alıcı hesap ({input.ReceiverIBAN}) ile daha önce onaylanmış işlem geçmişi bulunmamaktadır ve yüksek tutarlı (>= 15.000 TL) transfer denemesi yapılmaktadır.");
                    }
                }

                // SENARYO 20: Katır Hesap Bakiye Sapması (Pasif hesaba ani bakiye gelmesi)
                var receiverDebit = await _debitCardRepository.GetByIBANAsync(input.ReceiverIBAN);
                if (receiverDebit != null)
                {
                    var receiverRecentTx = await _transactionRepository.GetRecentTransactionsAsync(receiverDebit.CardId, TimeSpan.FromDays(30));
                    bool isPassiveAccount = !receiverRecentTx.Any(t => t.Status == "Approved");
                    if (isPassiveAccount && input.Amount >= 5000)
                    {
                        return ("RECEIVER_BALANCE_ANOMALY", $"Alıcı hesap ({input.ReceiverIBAN}) son 30 gündür pasif olmasına rağmen ani ve yüksek tutarlı (>= 5.000 TL) transfer gelmektedir.");
                    }
                }

                return (null, null);
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

using FraudGuard.Domain.Common.Constants;
using FraudGuard.Domain.Common.Enums;
using FraudGuard.Domain.DomainObjects.FraudEvaluation;
using FraudGuard.Domain.DomainObjects.TransactionProcessing;
using FraudGuard.Domain.Entities;
using FraudGuard.Domain.Interfaces.Abstractions;
using FraudGuard.Application.Interfaces;
using FraudGuard.Domain.Interfaces.DomainServices;
using FraudGuard.Domain.Interfaces.Entities;
using FraudGuard.Domain.Interfaces.Repositories;
using FraudGuard.Domain.Services.RuleEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FraudGuard.Application.Services
{
    /// <summary>
    /// Fraud değerlendirmesinin orkestratörü. Motorların sırasını ve veri akışını yönetir,
    /// kural mantığının kendisini içermez.
    /// <para>
    /// Akış: geçmiş yükle → sayaçları zenginleştir → tüm kuralları çalıştır →
    /// kombinasyon bonusu → güven indirimi → kademeli karar.
    /// </para>
    /// </summary>
    public class FraudEvaluationService : IFraudEvaluationService
    {
        private static readonly TimeSpan HistoryWindow = TimeSpan.FromHours(24);
        private static readonly TimeSpan HistoryCacheTtl = TimeSpan.FromMinutes(5);

        /// <summary>Alıcıya gelen transferlerin tarandığı pencere (çoklu gönderici tespiti).</summary>
        private static readonly TimeSpan ReceiverHistoryWindow = TimeSpan.FromHours(1);

        /// <summary>
        /// İşyeri geçmişinin tarandığı pencere. Kart geçmişinden uzundur: "30 gündür yüksek
        /// tutarlı işlem yok" ve "N gündür hiç işlem yok" tipolojileri bu genişliği gerektirir.
        /// Kısa pencereli sayaçlar (1-6-24 saat) aynı listeden süzülerek hesaplanır.
        /// </summary>
        private static readonly TimeSpan MerchantHistoryWindow = TimeSpan.FromDays(30);

        private readonly ITransactionRepository _transactionRepository;
        private readonly ICreditCardRepository _creditCardRepository;
        private readonly IDebitCardRepository _debitCardRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly IFraudRuleRepository _fraudRuleRepository;
        private readonly IFraudLogRepository _fraudLogRepository;
        private readonly IMerchantRepository _merchantRepository;
        private readonly IReferenceDataProvider _referenceDataProvider;
        private readonly IRuleCombinationRepository _combinationRepository;
        private readonly IDynamicRuleEngine _ruleEngine;
        private readonly ICombinationEngine _combinationEngine;
        private readonly ITrustScoreService _trustScoreService;
        private readonly IRiskScoringService _riskScoringService;
        private readonly ICacheProvider _cacheProvider;

        public FraudEvaluationService(
            ITransactionRepository transactionRepository,
            ICreditCardRepository creditCardRepository,
            IDebitCardRepository debitCardRepository,
            ICustomerRepository customerRepository,
            IFraudRuleRepository fraudRuleRepository,
            IFraudLogRepository fraudLogRepository,
            IMerchantRepository merchantRepository,
            IReferenceDataProvider referenceDataProvider,
            IRuleCombinationRepository combinationRepository,
            IDynamicRuleEngine ruleEngine,
            ICombinationEngine combinationEngine,
            ITrustScoreService trustScoreService,
            IRiskScoringService riskScoringService,
            ICacheProvider cacheProvider)
        {
            _transactionRepository = transactionRepository;
            _creditCardRepository = creditCardRepository;
            _debitCardRepository = debitCardRepository;
            _customerRepository = customerRepository;
            _fraudRuleRepository = fraudRuleRepository;
            _fraudLogRepository = fraudLogRepository;
            _merchantRepository = merchantRepository;
            _referenceDataProvider = referenceDataProvider;
            _combinationRepository = combinationRepository;
            _ruleEngine = ruleEngine;
            _combinationEngine = combinationEngine;
            _trustScoreService = trustScoreService;
            _riskScoringService = riskScoringService;
            _cacheProvider = cacheProvider;
        }

        public async Task<FraudDecisionResult> EvaluateAsync(
            ProcessTransactionInput input, int cardId, bool isCreditCard)
        {
            var history = await LoadRecentHistoryAsync(input);

            decimal cardLimit = 0m;
            decimal cardBalance = 0m;
            if (cardId > 0)
            {
                if (isCreditCard)
                {
                    var cc = await _creditCardRepository.GetByIdAsync(cardId);
                    if (cc != null)
                    {
                        cardLimit = cc.CardLimit;
                        cardBalance = cc.AvailableLimit;
                    }
                }
                else
                {
                    var dc = await _debitCardRepository.GetByIdAsync(cardId);
                    if (dc != null)
                    {
                        cardLimit = dc.Balance;
                        cardBalance = dc.Balance;
                    }
                }
            }

            // İşyeri seçilmeden gönderilen işlemlerde ikisi de null kalır; işyeri bazlı
            // sayaçlar hesaplanmaz ve onları kullanan kurallar tetiklenmez.
            var merchant = await _merchantRepository.GetByIdAsync(input.MerchantId ?? string.Empty);

            var merchantHistory = merchant is null
                ? null
                : await _transactionRepository.GetRecentTransactionsByMerchantAsync(
                    merchant.MerchantId, MerchantHistoryWindow);

            // Alıcı tarafına ait veri: enricher saf olduğu için repository'ye erişemez,
            // bu iki girdi burada toplanıp hazır geçilir. Yalnızca transfer işlemlerinde okunur.
            var (receiverHistory, isReceiverBlocked) = await LoadReceiverContextAsync(input);

            // BIN tablosu ve operasyonel listeler. Enricher saf olduğu için hazır geçilir.
            var referenceData = await _referenceDataProvider.GetAsync();

            TransactionInputEnricher.Enrich(
                input, history, cardLimit, cardBalance,
                evaluatedAt: null,
                merchant: merchant,
                merchantHistory: merchantHistory,
                cardId: cardId,
                isCreditCard: isCreditCard,
                receiverHistory: receiverHistory,
                isReceiverBlocked: isReceiverBlocked,
                referenceData: referenceData);

            var activeRules = await _fraudRuleRepository.GetAllActiveRulesAsync();
            var outcome = _ruleEngine.Evaluate(input, activeRules);

            if (outcome.Triggered.Count == 0)
                return FraudDecisionResult.Clean(outcome.Failures);

            var combinationDefinitions = await _combinationRepository.GetAllActiveAsync();
            var appliedCombinations = _combinationEngine.Evaluate(outcome.Triggered, combinationDefinitions);

            var trustContext = await BuildTrustContextAsync(cardId, isCreditCard);
            var trust = _trustScoreService.Evaluate(trustContext);

            // Skorlama politikası (eşikler, indirim, havuz ayrımı) Domain'e aittir;
            // bu servis yalnızca veriyi toplayıp motorların sırasını yönetir.
            return _riskScoringService.BuildDecision(outcome, appliedCombinations, trust);
        }

        // ------------------------------------------------------------------
        // Alıcı tarafı verisi
        // ------------------------------------------------------------------

        /// <summary>
        /// Alıcı hesabına ait fraud sinyallerini toplar.
        /// <para>
        /// Enricher saf bir domain servisidir ve repository'ye erişemez; alıcı tarafına bakan
        /// göstergelerin verisi bu yüzden burada toplanır. Transfer dışı işlemlerde sorgu yapılmaz.
        /// </para>
        /// </summary>
        /// <returns>Alıcıya gelen son 1 saatlik transferler ve alıcı hesabın bloke durumu.</returns>
        private async Task<(List<ITransaction>? ReceiverHistory, bool IsReceiverBlocked)>
            LoadReceiverContextAsync(ProcessTransactionInput input)
        {
            bool isTransfer = input.PaymentType is PaymentTypeEnum.EFT or PaymentTypeEnum.BankTransfer;

            if (!isTransfer || string.IsNullOrWhiteSpace(input.ReceiverIBAN))
                return (null, false);

            var incoming = await _transactionRepository
                .GetRecentTransferTransactionsByReceiverIBANAsync(input.ReceiverIBAN, ReceiverHistoryWindow);

            // Sistemde bloke edilmiş hesap, kara listenin ta kendisidir.
            var receiverAccount = await _debitCardRepository.GetByIBANAsync(input.ReceiverIBAN);

            return (incoming.Cast<ITransaction>().ToList(), receiverAccount?.IsBlocked ?? false);
        }

        // ------------------------------------------------------------------
        // Güven skoru verisinin toplanması
        // ------------------------------------------------------------------

        /// <summary>
        /// Güven faktörlerinin ham verisini toplar. Hesabı <see cref="ITrustScoreService"/> yapar.
        /// <para>
        /// İşyeri tarafı, Merchant master verisi sisteme eklenene kadar boş bırakılır;
        /// bu durumda işyeri indirimi uygulanmaz.
        /// </para>
        /// </summary>
        private async Task<TrustContext> BuildTrustContextAsync(int cardId, bool isCreditCard)
        {
            if (cardId <= 0)
                return new TrustContext();

            int? tenureDays = await ResolveCardHolderTenureAsync(cardId, isCreditCard);

            int alarmCount = await _fraudLogRepository.CountRecentAlarmsForCardAsync(
                cardId,
                isCreditCard,
                DateTime.Now.AddDays(-RiskScoringConstants.NoAlarmLookbackDays));

            return new TrustContext
            {
                CardHolderTenureDays = tenureDays,
                CardAlarmCountLast90Days = alarmCount,
                IsCardWhitelisted = false,

                // Merchant domain'i eklenmeden işyeri güven geçmişi bilinemez.
                MerchantTenureDays = null,
                MerchantAlarmCountLast90Days = null,
                IsMerchantWhitelisted = false
            };
        }

        private async Task<int?> ResolveCardHolderTenureAsync(int cardId, bool isCreditCard)
        {
            int customerId;

            if (isCreditCard)
            {
                var creditCard = await _creditCardRepository.GetByIdAsync(cardId);
                if (creditCard is null) return null;
                customerId = creditCard.CustomerId;
            }
            else
            {
                var debitCard = await _debitCardRepository.GetByIdAsync(cardId);
                if (debitCard is null) return null;
                customerId = debitCard.CustomerId;
            }

            var customer = await _customerRepository.GetByIdAsync(customerId);
            if (customer is null) return null;

            return (int)Math.Max(0, (DateTime.Now - customer.CreatedAt).TotalDays);
        }

        // ------------------------------------------------------------------
        // İşlem geçmişi
        // ------------------------------------------------------------------

        private async Task<List<ITransaction>> LoadRecentHistoryAsync(ProcessTransactionInput input)
        {
            string cacheKey = BuildHistoryCacheKey(input);

            if (string.IsNullOrEmpty(cacheKey))
                return new List<ITransaction>();

            var cached = await ReadHistoryFromCacheAsync(input, cacheKey);
            if (cached is not null)
                return cached;

            return await ReadHistoryFromStoreAsync(input, cacheKey);
        }

        private static string BuildHistoryCacheKey(ProcessTransactionInput input)
        {
            if (!string.IsNullOrEmpty(input.CardNumber))
                return $"recent_txs_{input.CardNumber}";

            if (!string.IsNullOrEmpty(input.SenderIBAN))
                return $"recent_txs_{input.SenderIBAN}";

            return string.Empty;
        }

        private async Task<List<ITransaction>?> ReadHistoryFromCacheAsync(
            ProcessTransactionInput input, string cacheKey)
        {
            if (!string.IsNullOrEmpty(input.CardNumber))
            {
                var creditCard = await _creditCardRepository.GetByCardNumberAsync(input.CardNumber);
                if (creditCard != null)
                {
                    var cached = await _cacheProvider.GetAsync<List<ECreditCardTransaction>>(cacheKey);
                    return cached?.Cast<ITransaction>().ToList();
                }

                var debitCard = await _debitCardRepository.GetByCardNumberAsync(input.CardNumber);
                if (debitCard != null)
                {
                    var cached = await _cacheProvider.GetAsync<List<EDebitCardTransaction>>(cacheKey);
                    return cached?.Cast<ITransaction>().ToList();
                }

                return null;
            }

            var cachedTransfers = await _cacheProvider.GetAsync<List<ETransferTransaction>>(cacheKey);
            return cachedTransfers?.Cast<ITransaction>().ToList();
        }

        private async Task<List<ITransaction>> ReadHistoryFromStoreAsync(
            ProcessTransactionInput input, string cacheKey)
        {
            if (!string.IsNullOrEmpty(input.CardNumber))
            {
                var creditCard = await _creditCardRepository.GetByCardNumberAsync(input.CardNumber);
                if (creditCard != null)
                {
                    var transactions = await _transactionRepository.GetRecentTransactionsAsync(
                        creditCard.CardId, isCreditCard: true, HistoryWindow);

                    if (transactions.Count > 0)
                    {
                        await _cacheProvider.SetAsync(
                            cacheKey, transactions.Cast<ECreditCardTransaction>().ToList(), HistoryCacheTtl);
                    }

                    return transactions;
                }

                var debitCard = await _debitCardRepository.GetByCardNumberAsync(input.CardNumber);
                if (debitCard != null)
                {
                    var transactions = await _transactionRepository.GetRecentTransactionsAsync(
                        debitCard.CardId, isCreditCard: false, HistoryWindow);

                    if (transactions.Count > 0)
                    {
                        await _cacheProvider.SetAsync(
                            cacheKey, transactions.Cast<EDebitCardTransaction>().ToList(), HistoryCacheTtl);
                    }

                    return transactions;
                }

                return new List<ITransaction>();
            }

            if (!string.IsNullOrEmpty(input.SenderIBAN))
            {
                var senderAccount = await _debitCardRepository.GetByIBANAsync(input.SenderIBAN);
                if (senderAccount != null)
                {
                    var transfers = await _transactionRepository
                        .GetRecentTransferTransactionsBySenderIBANAsync(input.SenderIBAN, HistoryWindow);

                    if (transfers.Count > 0)
                        await _cacheProvider.SetAsync(cacheKey, transfers, HistoryCacheTtl);

                    return transfers.Cast<ITransaction>().ToList();
                }
            }

            return new List<ITransaction>();
        }

        // ------------------------------------------------------------------
        // Fraud log
        // ------------------------------------------------------------------

        public async Task CreateFraudLogAsync(
            int transactionId, 
            string ruleCode, 
            PaymentTypeEnum paymentType,
            bool isAutoBlocked = false,
            string? resolvedBy = null,
            string? adminNote = null)
        {
            var rule = await _fraudRuleRepository.GetByCodeAsync(ruleCode);
            if (rule is null || !rule.IsActive)
                return;

            var log = new EFraudLog
            {
                RuleId = rule.RuleId,
                LogDate = DateTime.Now,
                IsResolved = isAutoBlocked,
                Status = isAutoBlocked ? FraudLogStatuses.Resolved : FraudLogStatuses.Unresolved,
                AdminAction = isAutoBlocked ? "BLOCKED" : null,
                ResolvedByAdmin = isAutoBlocked ? (resolvedBy ?? "Sistem (Otomatik Bloke)") : null,
                AdminNote = isAutoBlocked ? (adminNote ?? "Sistem tarafından şüpheli ve yüksek riskli bulunup direkt bloke edildi.") : null
            };

            switch (paymentType)
            {
                case PaymentTypeEnum.CreditCard:
                    log.CreditCardTransactionId = transactionId;
                    break;
                case PaymentTypeEnum.DebitCard:
                    log.DebitCardTransactionId = transactionId;
                    break;
                case PaymentTypeEnum.EFT:
                case PaymentTypeEnum.BankTransfer:
                    log.TransferTransactionId = transactionId;
                    break;
            }

            await _fraudLogRepository.AddAsync(log);
        }
    }
}

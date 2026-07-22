using FraudGuard.Domain.DomainObjects.TransactionProcessing;
using FraudGuard.Domain.Common.Enums;
using FraudGuard.Domain.Entities;
using FraudGuard.Domain.Interfaces.DomainServices;
using FraudGuard.Domain.Interfaces.Repositories;
using FraudGuard.Domain.Interfaces.Abstractions;
using FraudGuard.Domain.Interfaces.Rules;
using FraudGuard.Domain.Interfaces.Entities;
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
        private readonly IEnumerable<IFraudRule> _rules;
        private readonly ICacheProvider _cacheProvider;

        public FraudEvaluationService(
            ITransactionRepository transactionRepository,
            ICreditCardRepository creditCardRepository,
            IDebitCardRepository debitCardRepository,
            IFraudRuleRepository fraudRuleRepository,
            IFraudLogRepository fraudLogRepository,
            IEnumerable<IFraudRule> rules,
            ICacheProvider cacheProvider)
        {
            _transactionRepository = transactionRepository;
            _creditCardRepository = creditCardRepository;
            _debitCardRepository = debitCardRepository;
            _fraudRuleRepository = fraudRuleRepository;
            _fraudLogRepository = fraudLogRepository;
            _rules = rules;
            _cacheProvider = cacheProvider;
        }

        public async Task<(string? RuleCode, string? FraudReason)> EvaluateAsync(ProcessTransactionInput input, int cardId)
        {
            return await EvaluateAllRulesAsync(input);
        }

        public async Task<(string? RuleCode, string? FraudReason)> EvaluateAllRulesAsync(ProcessTransactionInput input)
        {
            List<ITransaction> recentTransactions = null!;
            string cacheKey = !string.IsNullOrEmpty(input.CardNumber) 
                ? $"recent_txs_{input.CardNumber}" 
                : (!string.IsNullOrEmpty(input.SenderIBAN) ? $"recent_txs_{input.SenderIBAN}" : string.Empty);

            if (!string.IsNullOrEmpty(cacheKey))
            {
                if (!string.IsNullOrEmpty(input.CardNumber))
                {
                    var cc = await _creditCardRepository.GetByCardNumberAsync(input.CardNumber);
                    if (cc != null)
                    {
                        var ccTxs = await _cacheProvider.GetAsync<List<ECreditCardTransaction>>(cacheKey);
                        if (ccTxs != null) recentTransactions = ccTxs.Cast<ITransaction>().ToList();
                    }
                    else
                    {
                        var dc = await _debitCardRepository.GetByCardNumberAsync(input.CardNumber);
                        if (dc != null)
                        {
                            var dcTxs = await _cacheProvider.GetAsync<List<EDebitCardTransaction>>(cacheKey);
                            if (dcTxs != null) recentTransactions = dcTxs.Cast<ITransaction>().ToList();
                        }
                    }
                }
                else if (!string.IsNullOrEmpty(input.SenderIBAN))
                {
                    var trans = await _cacheProvider.GetAsync<List<ETransferTransaction>>(cacheKey);
                    if (trans != null) recentTransactions = trans.Cast<ITransaction>().ToList();
                }
            }

            if (recentTransactions == null)
            {
                recentTransactions = new List<ITransaction>();
                if (!string.IsNullOrEmpty(input.CardNumber))
                {
                    var cc = await _creditCardRepository.GetByCardNumberAsync(input.CardNumber);
                    if (cc != null)
                    {
                        var ccTxs = await _transactionRepository.GetRecentTransactionsAsync(cc.CardId, isCreditCard: true, TimeSpan.FromHours(24));
                        recentTransactions = ccTxs;
                        if (!string.IsNullOrEmpty(cacheKey) && ccTxs.Count > 0)
                        {
                            await _cacheProvider.SetAsync(cacheKey, ccTxs.Cast<ECreditCardTransaction>().ToList(), TimeSpan.FromMinutes(5));
                        }
                    }
                    else
                    {
                        var dc = await _debitCardRepository.GetByCardNumberAsync(input.CardNumber);
                        if (dc != null)
                        {
                            var dcTxs = await _transactionRepository.GetRecentTransactionsAsync(dc.CardId, isCreditCard: false, TimeSpan.FromHours(24));
                            recentTransactions = dcTxs;
                            if (!string.IsNullOrEmpty(cacheKey) && dcTxs.Count > 0)
                            {
                                await _cacheProvider.SetAsync(cacheKey, dcTxs.Cast<EDebitCardTransaction>().ToList(), TimeSpan.FromMinutes(5));
                            }
                        }
                    }
                }
                else if (!string.IsNullOrEmpty(input.SenderIBAN))
                {
                    var dc = await _debitCardRepository.GetByIBANAsync(input.SenderIBAN);
                    if (dc != null)
                    {
                        var transTxs = await _transactionRepository.GetRecentTransferTransactionsBySenderIBANAsync(input.SenderIBAN, TimeSpan.FromHours(24));
                        recentTransactions = transTxs.Cast<ITransaction>().ToList();
                        if (!string.IsNullOrEmpty(cacheKey) && transTxs.Count > 0)
                        {
                            await _cacheProvider.SetAsync(cacheKey, transTxs, TimeSpan.FromMinutes(5));
                        }
                    }
                }
            }

            var activeRules = await _fraudRuleRepository.GetAllActiveRulesAsync();
            var activeRuleCodes = activeRules.Where(r => r.IsActive).Select(r => r.RuleCode).ToHashSet();

            foreach (var rule in _rules)
            {
                if (activeRuleCodes.Contains(rule.RuleCode))
                {
                    // İade işlemleri sadece iade kurallarına (CONSECUTIVE_REFUNDS, HIGH_VALUE_REFUND_VOID) tabi tutulsun
                    if (input.TransactionType == TransactionTypeEnum.Refund && 
                        rule.RuleCode != "CONSECUTIVE_REFUNDS" && 
                        rule.RuleCode != "HIGH_VALUE_REFUND_VOID")
                    {
                        continue;
                    }

                    // Satış işlemleri iade kurallarından muaf tutulsun
                    if (input.TransactionType == TransactionTypeEnum.Sale && 
                        (rule.RuleCode == "CONSECUTIVE_REFUNDS" || rule.RuleCode == "HIGH_VALUE_REFUND_VOID"))
                    {
                        continue;
                    }

                    var (isSuspicious, reason) = await rule.EvaluateAsync(input, recentTransactions);
                    if (isSuspicious)
                    {
                        return (rule.RuleCode, reason);
                    }
                }
            }

            return (null, null);
        }

        public async Task CreateFraudLogAsync(int transactionId, string ruleCode, PaymentTypeEnum paymentType)
        {
            var rule = await _fraudRuleRepository.GetByCodeAsync(ruleCode);
            if (rule != null && rule.IsActive)
            {
                var log = new EFraudLog
                {
                    RuleId = rule.RuleId,
                    LogDate = DateTime.Now,
                    Status = "Unresolved"
                };

                if (paymentType == PaymentTypeEnum.CreditCard)
                {
                    log.CreditCardTransactionId = transactionId;
                }
                else if (paymentType == PaymentTypeEnum.DebitCard)
                {
                    log.DebitCardTransactionId = transactionId;
                }
                else if (paymentType == PaymentTypeEnum.EFT || paymentType == PaymentTypeEnum.BankTransfer)
                {
                    log.TransferTransactionId = transactionId;
                }

                await _fraudLogRepository.AddAsync(log);
            }
        }
    }
}

using FraudGuard.Domain.DomainObjects.TransactionProcessing;
using FraudGuard.Domain.Entities;
using FraudGuard.Domain.Interfaces.DomainServices;
using FraudGuard.Domain.Interfaces.Repositories;
using FraudGuard.Domain.Interfaces.Abstractions;
using FraudGuard.Domain.Interfaces.Rules;
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
            List<ETransaction> recentTransactions = null!;
            string cacheKey = !string.IsNullOrEmpty(input.CardNumber) 
                ? $"recent_txs_{input.CardNumber}" 
                : (!string.IsNullOrEmpty(input.SenderIBAN) ? $"recent_txs_{input.SenderIBAN}" : string.Empty);

            if (!string.IsNullOrEmpty(cacheKey))
            {
                recentTransactions = await _cacheProvider.GetAsync<List<ETransaction>>(cacheKey);
            }

            if (recentTransactions == null)
            {
                recentTransactions = new List<ETransaction>();
                if (!string.IsNullOrEmpty(input.CardNumber))
                {
                    var cc = await _creditCardRepository.GetByCardNumberAsync(input.CardNumber);
                    if (cc != null)
                    {
                        recentTransactions = await _transactionRepository.GetRecentTransactionsAsync(cc.CardId, TimeSpan.FromHours(24));
                    }
                    else
                    {
                        var dc = await _debitCardRepository.GetByCardNumberAsync(input.CardNumber);
                        if (dc != null)
                        {
                            recentTransactions = await _transactionRepository.GetRecentTransactionsAsync(dc.CardId, TimeSpan.FromHours(24));
                        }
                    }
                }
                else if (!string.IsNullOrEmpty(input.SenderIBAN))
                {
                    var dc = await _debitCardRepository.GetByIBANAsync(input.SenderIBAN);
                    if (dc != null)
                    {
                        recentTransactions = await _transactionRepository.GetRecentTransactionsAsync(dc.CardId, TimeSpan.FromHours(24));
                    }
                }

                if (!string.IsNullOrEmpty(cacheKey) && recentTransactions.Count > 0)
                {
                    await _cacheProvider.SetAsync(cacheKey, recentTransactions, TimeSpan.FromMinutes(5));
                }
            }

            var activeRules = await _fraudRuleRepository.GetAllActiveRulesAsync();
            var activeRuleCodes = activeRules.Where(r => r.IsActive).Select(r => r.RuleCode).ToHashSet();

            foreach (var rule in _rules)
            {
                if (activeRuleCodes.Contains(rule.RuleCode))
                {
                    var (isSuspicious, reason) = await rule.EvaluateAsync(input, recentTransactions);
                    if (isSuspicious)
                    {
                        return (rule.RuleCode, reason);
                    }
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

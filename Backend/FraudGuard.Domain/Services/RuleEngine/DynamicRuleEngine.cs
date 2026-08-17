using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FraudGuard.Domain.Common.Enums;
using FraudGuard.Domain.DomainObjects.FraudEvaluation;
using FraudGuard.Domain.DomainObjects.TransactionProcessing;
using FraudGuard.Domain.Entities;
using FraudGuard.Domain.Interfaces.Abstractions;
using FraudGuard.Domain.Interfaces.DomainServices;
using FraudGuard.Domain.Interfaces.Entities;
using FraudGuard.Domain.Interfaces.Rules;

namespace FraudGuard.Domain.Services.RuleEngine
{
    /// <summary>
    /// Kural motorunun çekirdeği. Aktif kuralların tamamını çalıştırır ve tetiklenenleri toplar.
    /// <para>
    /// İki kural tipini tek hatta birleştirir:
    /// <list type="bullet">
    /// <item><b>Dinamik</b> — <c>EFraudRule.Expression</c> derlenip çalıştırılır. Yeni kural eklemek
    /// için kod yazmak gerekmez, veritabanına satır eklemek yeterlidir.</item>
    /// <item><b>Kod tabanlı</b> — ifadeye sığmayacak kadar karmaşık kurallar için mevcut
    /// <see cref="IFraudRule"/> implementasyonları çalıştırılır.</item>
    /// </list>
    /// Her iki tip de aynı ceza puanını aynı havuza yazar.
    /// </para>
    /// </summary>
    public class DynamicRuleEngine : IDynamicRuleEngine
    {
        private readonly IRuleExpressionCompiler _compiler;
        private readonly IRuleDiagnostics _diagnostics;
        private readonly IReadOnlyDictionary<string, IFraudRule> _codeBasedRules;

        /// <summary>Yalnızca iade işlemlerinde çalışması gereken kurallar.</summary>
        private static readonly HashSet<string> RefundOnlyRuleCodes =
            new(StringComparer.OrdinalIgnoreCase) { "CONSECUTIVE_REFUNDS", "HIGH_VALUE_REFUND_VOID" };

        public DynamicRuleEngine(
            IRuleExpressionCompiler compiler,
            IRuleDiagnostics diagnostics,
            IEnumerable<IFraudRule>? codeBasedRules = null)
        {
            _compiler = compiler;
            _diagnostics = diagnostics;
            _codeBasedRules = (codeBasedRules ?? Enumerable.Empty<IFraudRule>())
                .GroupBy(r => r.RuleCode, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        }

        public async Task<RuleEvaluationOutcome> EvaluateAsync(
            ProcessTransactionInput input,
            IReadOnlyList<EFraudRule> activeRules,
            List<ITransaction> history)
        {
            var triggered = new List<TriggeredRule>();
            var failures = new List<RuleFailure>();

            foreach (var rule in activeRules)
            {
                if (!rule.IsActive || !IsApplicable(rule, input))
                    continue;

                try
                {
                    var evaluated = rule.IsExpressionBased
                        ? EvaluateExpressionRule(rule, input)
                        : await EvaluateCodeBasedRuleAsync(rule, input, history);

                    if (evaluated is not null)
                        triggered.Add(evaluated);
                }
                catch (Exception ex)
                {
                    // Tek bir bozuk kural ödeme akışını düşürmemeli — ama sessizce de kaybolmamalı.
                    // Hata hem loga hem karar sonucuna yazılır, kural atlanır.
                    _diagnostics.RuleEvaluationFailed(rule.RuleCode, rule.Expression, ex);

                    failures.Add(new RuleFailure
                    {
                        RuleCode = rule.RuleCode,
                        Expression = rule.Expression,
                        Error = ex.Message
                    });
                }
            }

            return new RuleEvaluationOutcome
            {
                Triggered = triggered,
                Failures = failures
            };
        }

        private TriggeredRule? EvaluateExpressionRule(EFraudRule rule, ProcessTransactionInput input)
        {
            var predicate = _compiler.Compile(rule.Expression!);

            if (!predicate(input))
                return null;

            return new TriggeredRule
            {
                RuleCode = rule.RuleCode,
                RuleName = rule.RuleName,
                Score = rule.Score,
                Target = rule.Target,
                Category = rule.Category,
                Reason = rule.Description ?? rule.RuleName,
                IsExpressionBased = true
            };
        }

        private async Task<TriggeredRule?> EvaluateCodeBasedRuleAsync(
            EFraudRule rule,
            ProcessTransactionInput input,
            List<ITransaction> history)
        {
            if (!_codeBasedRules.TryGetValue(rule.RuleCode, out var implementation))
                return null;

            var (isSuspicious, reason) = await implementation.EvaluateAsync(input, history);

            if (!isSuspicious)
                return null;

            return new TriggeredRule
            {
                RuleCode = rule.RuleCode,
                RuleName = rule.RuleName,
                Score = rule.Score,
                Target = rule.Target,
                Category = rule.Category,
                Reason = reason ?? rule.Description ?? rule.RuleName,
                IsExpressionBased = false
            };
        }

        /// <summary>
        /// İade ve satış kurallarının birbirine karışmasını engeller.
        /// İade işlemleri yalnızca iade kurallarına, satışlar iade kuralları dışındakilere tabidir.
        /// </summary>
        private static bool IsApplicable(EFraudRule rule, ProcessTransactionInput input)
        {
            bool isRefundRule = RefundOnlyRuleCodes.Contains(rule.RuleCode);

            return input.TransactionType switch
            {
                TransactionTypeEnum.Refund => isRefundRule,
                TransactionTypeEnum.Sale => !isRefundRule,
                _ => !isRefundRule
            };
        }
    }
}

using System;
using System.Collections.Generic;
using FraudGuard.Domain.Common.Enums;
using FraudGuard.Domain.DomainObjects.FraudEvaluation;
using FraudGuard.Domain.DomainObjects.TransactionProcessing;
using FraudGuard.Domain.Entities;
using FraudGuard.Domain.Interfaces.Abstractions;
using FraudGuard.Domain.Interfaces.DomainServices;

namespace FraudGuard.Domain.Services.RuleEngine
{
    /// <summary>
    /// Kural motorunun çekirdeği. Aktif kuralların tamamını çalıştırır ve tetiklenenleri toplar;
    /// ilk eşleşmede durmaz, çünkü karar kümülatif skora göre verilir.
    /// <para>
    /// Her kural bir <c>EFraudRule.Expression</c> ifadesidir; çalışma anında derlenip çalıştırılır.
    /// Yeni kural eklemek için kod yazmak gerekmez, veritabanına satır eklemek yeterlidir.
    /// </para>
    /// <para>
    /// Motor saf CPU işi yapar: veriye erişmez, giriş olarak zenginleştirilmiş
    /// <see cref="ProcessTransactionInput"/> alır.
    /// </para>
    /// </summary>
    public class DynamicRuleEngine : IDynamicRuleEngine
    {
        private readonly IRuleExpressionCompiler _compiler;
        private readonly IRuleDiagnostics _diagnostics;

        /// <summary>Yalnızca iade işlemlerinde çalışması gereken kurallar.</summary>
        private static readonly HashSet<string> RefundOnlyRuleCodes =
            new(StringComparer.OrdinalIgnoreCase) { "CONSECUTIVE_REFUNDS", "HIGH_VALUE_REFUND_VOID" };

        public DynamicRuleEngine(IRuleExpressionCompiler compiler, IRuleDiagnostics diagnostics)
        {
            _compiler = compiler;
            _diagnostics = diagnostics;
        }

        public RuleEvaluationOutcome Evaluate(
            ProcessTransactionInput input,
            IReadOnlyList<EFraudRule> activeRules)
        {
            var triggered = new List<TriggeredRule>();
            var failures = new List<RuleFailure>();

            foreach (var rule in activeRules)
            {
                if (!rule.IsActive || !IsApplicable(rule, input))
                    continue;

                try
                {
                    if (!rule.IsExpressionBased)
                    {
                        // İfadesiz kural çalıştırılamaz. Sessizce atlanması, kuralın çalıştığı
                        // yanılgısına yol açacağı için tanım hatası olarak raporlanır.
                        throw new InvalidOperationException(
                            "Kuralın ifadesi tanımlanmamış; ifadesiz kural değerlendirilemez.");
                    }

                    var evaluated = EvaluateRule(rule, input);
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

        private TriggeredRule? EvaluateRule(EFraudRule rule, ProcessTransactionInput input)
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
                IsCritical = rule.IsCritical,
                Reason = rule.Description ?? rule.RuleName
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
                _ => !isRefundRule
            };
        }
    }
}

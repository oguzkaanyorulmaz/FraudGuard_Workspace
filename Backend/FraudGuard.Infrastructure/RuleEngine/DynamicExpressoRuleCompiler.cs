using System;
using System.Collections.Concurrent;
using DynamicExpresso;
using FraudGuard.Domain.DomainObjects.TransactionProcessing;
using FraudGuard.Domain.Interfaces.Abstractions;

namespace FraudGuard.Infrastructure.RuleEngine
{
    /// <summary>
    /// <see cref="IRuleExpressionCompiler"/> implementasyonu. String ifadeyi bir kez derler,
    /// sonuçta oluşan delegate'i süreç ömrü boyunca önbellekte tutar.
    /// <para>
    /// Performans notu: DynamicExpresso ifadeyi Expression Tree'ye çevirip <c>Compile()</c> eder;
    /// oluşan delegate JIT sonrası native hıza yakın çalışır. Maliyet yalnızca ilk derlemededir,
    /// sonraki çağrılar sözlük araması + delegate çağrısıdır.
    /// </para>
    /// <para>
    /// Güvenlik notu: ifadeler yönetici tarafından veritabanına yazılır ve yazma anında
    /// <see cref="TryValidate"/> ile doğrulanır. Interpreter yalnızca <c>input</c> parametresine
    /// erişebilecek şekilde kurulur; yansıma veya I/O tipleri referanslanmaz.
    /// </para>
    /// </summary>
    public class DynamicExpressoRuleCompiler : IRuleExpressionCompiler
    {
        private const string ParameterName = "input";

        private readonly ConcurrentDictionary<string, Func<ProcessTransactionInput, bool>> _cache = new(StringComparer.Ordinal);
        private readonly Interpreter _interpreter;
        private readonly object _parseLock = new();

        public DynamicExpressoRuleCompiler()
        {
            _interpreter = new Interpreter(InterpreterOptions.Default);
            _interpreter.Reference(typeof(ProcessTransactionInput));
        }

        public Func<ProcessTransactionInput, bool> Compile(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
                throw new RuleCompilationException(expression ?? string.Empty, "İfade boş olamaz.");

            return _cache.GetOrAdd(expression, Parse);
        }

        public bool TryValidate(string expression, out string? error)
        {
            error = null;

            if (string.IsNullOrWhiteSpace(expression))
            {
                error = "İfade boş olamaz.";
                return false;
            }

            try
            {
                _cache.GetOrAdd(expression, Parse);
                return true;
            }
            catch (RuleCompilationException ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private Func<ProcessTransactionInput, bool> Parse(string expression)
        {
            try
            {
                // Interpreter örneği paylaşıldığı için ayrıştırma serileştirilir.
                // Sonuç önbelleğe alındığından bu kilit yalnızca ilk derlemede görülür.
                lock (_parseLock)
                {
                    var parameter = new Parameter(ParameterName, typeof(ProcessTransactionInput));
                    var lambda = _interpreter.Parse(expression, parameter);

                    if (lambda.ReturnType != typeof(bool))
                    {
                        throw new RuleCompilationException(
                            expression,
                            $"Kural ifadesi bool dönmelidir, dönen tip: {lambda.ReturnType.Name}.");
                    }

                    var compiled = lambda.Compile<Func<ProcessTransactionInput, bool>>();
                    return compiled;
                }
            }
            catch (RuleCompilationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new RuleCompilationException(
                    expression,
                    $"Kural ifadesi derlenemedi: {ex.Message}",
                    ex);
            }
        }
    }
}

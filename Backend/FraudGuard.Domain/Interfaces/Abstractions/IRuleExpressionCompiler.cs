using System;
using FraudGuard.Domain.DomainObjects.TransactionProcessing;

namespace FraudGuard.Domain.Interfaces.Abstractions
{
    /// <summary>
    /// String ifadeleri çalıştırılabilir predicate'e çeviren derleyici soyutlaması.
    /// <para>
    /// Domain katmanı ifade sözdizimini bilmez; hangi kütüphanenin kullanıldığı
    /// Infrastructure'ın kararıdır. Bu sayede Domain harici paket bağımlılığı taşımaz.
    /// </para>
    /// </summary>
    public interface IRuleExpressionCompiler
    {
        /// <summary>
        /// İfadeyi derler ve çalıştırılabilir bir predicate döner.
        /// Derlenen delegate önbelleğe alınmalıdır; aynı ifade tekrar derlenmez.
        /// </summary>
        /// <param name="expression">
        /// Tek parametresi <c>input</c> olan boolean ifade.
        /// Örn: <c>input.AyniKartIslemAdedi &gt;= 3</c>
        /// </param>
        /// <exception cref="RuleCompilationException">İfade derlenemezse fırlatılır.</exception>
        Func<ProcessTransactionInput, bool> Compile(string expression);

        /// <summary>
        /// İfadeyi doğrular. Kural kaydedilmeden önce sözdizimi kontrolü için kullanılır.
        /// Fırlatmaz; sonucu döner.
        /// </summary>
        bool TryValidate(string expression, out string? error);
    }

    /// <summary>
    /// Bir kural ifadesi derlenemediğinde fırlatılır.
    /// </summary>
    public class RuleCompilationException : Exception
    {
        public string Expression { get; }

        public RuleCompilationException(string expression, string message, Exception? inner = null)
            : base(message, inner)
        {
            Expression = expression;
        }
    }
}

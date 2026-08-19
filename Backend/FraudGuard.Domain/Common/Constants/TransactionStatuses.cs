namespace FraudGuard.Domain.Common.Constants
{
    /// <summary>
    /// İşlem durum değerleri. Veritabanında metin olarak saklandığı için enum yerine
    /// sabit kullanılır; <c>const</c> olmaları EF Core sorgularında doğrudan gömülmelerini sağlar.
    /// </summary>
    public static class TransactionStatuses
    {
        public const string Approved = "Approved";
        public const string Declined = "Declined";
        public const string Suspicious = "Suspicious";

        /// <summary>Eski kayıtlarda görülen iade-şüpheli durumu. Yeni işlemlerde üretilmez.</summary>
        public const string SuspiciousRefund = "SuspiciousRefund";
    }

    /// <summary>Fraud alarm kaydının çözüm durumu.</summary>
    public static class FraudLogStatuses
    {
        public const string Unresolved = "Unresolved";
        public const string Resolved = "Resolved";
    }

    /// <summary>Analistin bir alarm üzerinde alabileceği aksiyonlar.</summary>
    public static class AdminActions
    {
        public const string Approved = "Approved";
        public const string CardBlocked = "CardBlocked";
    }
}

namespace FraudGuard.Domain.Common.Enums
{
    public enum TransactionTypeEnum
    {
        Sale = 1,         // Satış
        Refund = 2,       // İade
        Deposit = 3,      // 📥 ATM Para Yatırma (Hesaba para giriş)
        CardPayment = 4   // 💰 Kredi Kartı Borç Ödeme (Limit açma)
    }
}

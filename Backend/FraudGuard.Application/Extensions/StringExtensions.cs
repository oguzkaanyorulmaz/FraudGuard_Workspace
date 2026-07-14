namespace FraudGuard.Application.Extensions
{
    public static class StringExtensions
    {
        public static string MaskCardNumber(this string cardNumber)
        {
            if (string.IsNullOrEmpty(cardNumber) || cardNumber.Length != 16)
                return cardNumber;

            return $"{cardNumber.Substring(0, 4)}********{cardNumber.Substring(12, 4)}";
        }
    }
}
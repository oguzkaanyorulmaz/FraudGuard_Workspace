namespace FraudGuard.Application.Helpers
{
    public static class MaskingExtensions
    {
        /// <summary>
        /// "1234567812345678" → "123456******5678"
        /// </summary>
        public static string MaskCardNumber(this string cardNumber)
        {
            if (string.IsNullOrEmpty(cardNumber) || cardNumber.Length < 12)
                return cardNumber;
            return cardNumber.Substring(0, 6) + "******" + cardNumber.Substring(cardNumber.Length - 4);
        }

        /// <summary>
        /// "12345678901" → "*******8901"
        /// </summary>
        public static string MaskIdentityNumber(this string identity)
        {
            if (string.IsNullOrEmpty(identity) || identity.Length < 4)
                return identity;
            return new string('*', identity.Length - 4) + identity.Substring(identity.Length - 4);
        }

        /// <summary>
        /// IBAN maskesi: "TR11 0006 2000 0000 0001 0000 01" → "TR1100************0001"
        /// <para>
        /// Kart maskesiyle aynı şey değildir: IBAN'ın ilk 4 hanesi ülke ve kontrol kodudur,
        /// kurum bilgisi taşımaz; ortadaki hesap numarası gizlenir.
        /// </para>
        /// </summary>
        public static string? MaskIban(this string? iban)
        {
            if (string.IsNullOrEmpty(iban) || iban.Length < 10)
                return iban;

            return iban.Substring(0, 4) + new string('*', iban.Length - 8) + iban.Substring(iban.Length - 4);
        }

        /// <summary>
        /// "+905555555555" → "+90*****5555"
        /// </summary>
        public static string? MaskPhoneNumber(this string? phone)
        {
            if (string.IsNullOrEmpty(phone) || phone.Length < 7)
                return phone;
            return phone.Substring(0, 3) + "*****" + phone.Substring(phone.Length - 4);
        }
    }
}

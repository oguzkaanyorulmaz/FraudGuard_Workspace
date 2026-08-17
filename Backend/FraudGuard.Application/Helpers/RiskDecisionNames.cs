using FraudGuard.Domain.Common.Enums;

namespace FraudGuard.Application.Helpers
{
    /// <summary>
    /// Karar enum'unu istemcinin beklediği sabit metin biçimine çevirir.
    /// Enum isimleri değişse bile dış sözleşme sabit kalsın diye ayrı tutulmuştur.
    /// </summary>
    public static class RiskDecisionNames
    {
        public const string Normal = "NORMAL";
        public const string Izle = "IZLE";
        public const string EkDogrulama = "EK_DOGRULAMA";
        public const string RetBloke = "RET_BLOKE";

        public static string ToWireFormat(RiskDecisionEnum decision) => decision switch
        {
            RiskDecisionEnum.RetBloke => RetBloke,
            RiskDecisionEnum.EkDogrulama => EkDogrulama,
            RiskDecisionEnum.Izle => Izle,
            _ => Normal
        };
    }
}

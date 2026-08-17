namespace FraudGuard.Domain.Common.Enums
{
    /// <summary>
    /// Bir kuralın ürettiği ceza puanının hangi risk havuzuna yazılacağını belirler.
    /// Kart ve işyeri skorları birbirinden bağımsız birikir.
    /// </summary>
    public enum RuleTargetEnum
    {
        /// <summary>Puan kart risk skoruna yazılır.</summary>
        Card = 1,

        /// <summary>Puan işyeri risk skoruna yazılır.</summary>
        Merchant = 2
    }
}

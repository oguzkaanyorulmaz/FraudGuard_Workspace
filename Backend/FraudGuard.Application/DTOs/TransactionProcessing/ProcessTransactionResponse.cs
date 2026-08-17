using System.Collections.Generic;

namespace FraudGuard.Application.DTOs.TransactionProcessing
{
    /// <summary>
    /// İşlem sonucunun istemciye dönen tam görünümü.
    /// Nihai kararın yanında ona nasıl ulaşıldığını da taşır; analist panelinde skor
    /// kırılımının gösterilebilmesi buna bağlıdır.
    /// </summary>
    public class ProcessTransactionResponse
    {
        public int? TransactionId { get; set; }

        /// <summary>Approved / Declined / Suspicious.</summary>
        public string Status { get; set; } = string.Empty;

        public string DeclineReason { get; set; } = string.Empty;

        public string RRN { get; set; } = string.Empty;

        // --- Fraud kararı ---

        /// <summary>NORMAL / IZLE / EK_DOGRULAMA / RET_BLOKE.</summary>
        public string Decision { get; set; } = "NORMAL";

        /// <summary>Kararı belirleyen skor: kart ve işyeri skorlarının büyüğü.</summary>
        public int RiskScore { get; set; }

        public int CardRiskScore { get; set; }

        public int MerchantRiskScore { get; set; }

        /// <summary>İndirim öncesi ham kural puanı toplamı.</summary>
        public int RawRuleScore { get; set; }

        /// <summary>Uygulanan toplam kombinasyon bonusu.</summary>
        public int TotalBonusScore { get; set; }

        /// <summary>Uygulanan toplam güven indirimi.</summary>
        public int TotalTrustDiscount { get; set; }

        /// <summary>Ek doğrulama (3D Secure / OTP) gerekip gerekmediği.</summary>
        public bool RequiresAdditionalVerification { get; set; }

        public List<TriggeredRuleDto> TriggeredRules { get; set; } = new();

        public List<AppliedCombinationDto> AppliedCombinations { get; set; } = new();

        /// <summary>Uygulanan güven faktörlerinin okunabilir listesi.</summary>
        public List<string> TrustFactors { get; set; } = new();

        /// <summary>
        /// Bu değerlendirmede çalıştırılamayan kurallar. Dolu olması, kural kataloğunda
        /// düzeltilmesi gereken bir tanım olduğu anlamına gelir — kural sessizce atlanmıştır.
        /// </summary>
        public List<RuleFailureDto> RuleFailures { get; set; } = new();
    }
}

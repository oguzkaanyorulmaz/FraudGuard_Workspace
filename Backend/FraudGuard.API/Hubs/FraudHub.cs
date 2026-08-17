using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace FraudGuard.API.Hubs
{
    /// <summary>
    /// Analist panelinin canlı bildirim kanalı.
    /// </summary>
    /// <remarks>
    /// İki olay yayınlanır:
    /// <list type="bullet">
    /// <item><c>RefreshLogs</c> — mevcut istemcinin listeyi yenilemesi için kullanılan sinyal.
    /// Geriye dönük uyumluluk için korunmuştur.</item>
    /// <item><c>FraudDecision</c> — kararın tam kırılımını taşıyan zengin paket.</item>
    /// </list>
    /// </remarks>
    public class FraudHub : Hub<IFraudHubClient>
    {
    }

    /// <summary>
    /// Hub'ın istemciye çağırabileceği metotların tip güvenli sözleşmesi.
    /// </summary>
    public interface IFraudHubClient
    {
        Task RefreshLogs();

        Task FraudDecision(FraudDecisionNotification notification);
    }

    /// <summary>
    /// Karar anında panele gönderilen zengin veri paketi.
    /// </summary>
    public class FraudDecisionNotification
    {
        public int? TransactionId { get; set; }
        public string RRN { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;

        /// <summary>NORMAL / IZLE / EK_DOGRULAMA / RET_BLOKE.</summary>
        public string Decision { get; set; } = "NORMAL";

        public int RiskScore { get; set; }
        public int CardRiskScore { get; set; }
        public int MerchantRiskScore { get; set; }
        public int RawRuleScore { get; set; }
        public int TotalBonusScore { get; set; }
        public int TotalTrustDiscount { get; set; }
        public bool RequiresAdditionalVerification { get; set; }

        public object[] TriggeredRules { get; set; } = [];
        public object[] AppliedCombinations { get; set; } = [];
        public string[] TrustFactors { get; set; } = [];

        public string OccurredAt { get; set; } = string.Empty;
    }
}

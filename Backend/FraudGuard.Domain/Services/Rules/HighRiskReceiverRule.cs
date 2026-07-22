using FraudGuard.Domain.Interfaces.Entities;
using FraudGuard.Domain.DomainObjects.TransactionProcessing;
using FraudGuard.Domain.Entities;
using FraudGuard.Domain.Interfaces.Rules;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FraudGuard.Domain.Services.Rules
{
    public class HighRiskReceiverRule : IFraudRule
    {
        public string RuleCode => "HIGH_RISK_RECEIVER";
        public string RuleName => "Şüpheli Alıcı Hesabı / Katır Hesap (EFT/Havale)";

        public Task<(bool IsSuspicious, string? Reason)> EvaluateAsync(ProcessTransactionInput input, List<ITransaction> history)
        {
            if (!string.IsNullOrEmpty(input.ReceiverIBAN))
            {
                string[] blacklistedIbans = { "TR99000620000000000999999", "TR88000620000000000888888" };
                if (blacklistedIbans.Contains(input.ReceiverIBAN))
                {
                    return Task.FromResult((true, (string?)$"Alıcı hesap ({input.ReceiverIBAN}) sistemde şüpheli/katır hesap olarak işaretlenmiştir."));
                }
            }

            return Task.FromResult((false, (string?)null));
        }
    }
}
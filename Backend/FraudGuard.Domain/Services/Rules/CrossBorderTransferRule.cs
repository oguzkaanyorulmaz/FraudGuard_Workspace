using FraudGuard.Domain.Common.Enums;
using FraudGuard.Domain.DomainObjects.TransactionProcessing;
using FraudGuard.Domain.Entities;
using FraudGuard.Domain.Interfaces.Rules;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FraudGuard.Domain.Services.Rules
{
    public class CrossBorderTransferRule : IFraudRule
    {
        public string RuleCode => "CROSS_BORDER_TRANSFER";
        public string RuleName => "Yurt Dışı Havale / EFT Anormalliği (Cross-Border Transfer)";

        public Task<(bool IsSuspicious, string? Reason)> EvaluateAsync(ProcessTransactionInput input, List<ETransaction> history)
        {
            if (input.PaymentType == PaymentTypeEnum.EFT || input.PaymentType == PaymentTypeEnum.BankTransfer)
            {
                if (input.Country != "Türkiye" && input.Amount >= 20000)
                {
                    bool hasInternationalTransferBefore = history.Any(t => t.Country != "Türkiye" && (t.PaymentType == PaymentTypeEnum.BankTransfer || t.PaymentType == PaymentTypeEnum.EFT));
                    if (!hasInternationalTransferBefore)
                    {
                        return Task.FromResult((true, (string?)"Hesap geçmişinde yurt dışı transfer kaydı bulunmayan hesaptan aniden yurt dışına limit üstü transfer."));
                    }
                }
            }

            return Task.FromResult((false, (string?)null));
        }
    }
}

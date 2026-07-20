using FraudGuard.Domain.Common.Enums;
using FraudGuard.Domain.DomainObjects.TransactionProcessing;
using FraudGuard.Domain.Entities;
using FraudGuard.Domain.Interfaces.Rules;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FraudGuard.Domain.Services.Rules
{
    public class NewBeneficiaryTransferRule : IFraudRule
    {
        public string RuleCode => "NEW_BENEFICIARY_TRANSFER";
        public string RuleName => "Yeni Alıcı Transfer Anormalliği";

        public Task<(bool IsSuspicious, string? Reason)> EvaluateAsync(ProcessTransactionInput input, List<ETransaction> history)
        {
            if (input.PaymentType == PaymentTypeEnum.EFT || input.PaymentType == PaymentTypeEnum.BankTransfer)
            {
                if (input.Amount >= 15000)
                {
                    bool hasPriorTransferToThisReceiver = history.Any(t => 
                        t.ReceiverIBAN == input.ReceiverIBAN && 
                        t.Status == "Approved");
                    if (!hasPriorTransferToThisReceiver)
                    {
                        return Task.FromResult((true, (string?)$"Alıcı hesap ({input.ReceiverIBAN}) ile daha önce onaylanmış işlem geçmişi bulunmamaktadır ve yüksek tutarlı (>= 15.000 TL) transfer denemesi yapılmaktadır."));
                    }
                }
            }

            return Task.FromResult((false, (string?)null));
        }
    }
}

using FraudGuard.Domain.Interfaces.Entities;
using FraudGuard.Domain.Common.Enums;
using FraudGuard.Domain.DomainObjects.TransactionProcessing;
using FraudGuard.Domain.Entities;
using FraudGuard.Domain.Interfaces.Rules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FraudGuard.Domain.Services.Rules
{
    public class WalletCashoutRule : IFraudRule
    {
        public string RuleCode => "WALLET_CASHOUT";
        public string RuleName => "Wallet Cash-Out";

        public Task<(bool IsSuspicious, string? Reason)> EvaluateAsync(ProcessTransactionInput input, List<ITransaction> history)
        {
            if (input.PaymentType == PaymentTypeEnum.EFT || input.PaymentType == PaymentTypeEnum.BankTransfer)
            {
                var hasRecentIncomingLoad = history.Any(t => 
                    t.TransactionDate <= DateTime.Now &&
                    (DateTime.Now - t.TransactionDate).TotalMinutes <= 15 && 
                    t.Status == "Approved" && 
                    t.TransactionTypeId == 1);
                if (hasRecentIncomingLoad)
                {
                    return Task.FromResult((true, (string?)"Son 15 dakika içinde karta/hesaba bakiye yüklemesi yapılmasının ardından hemen EFT ile çıkış denemesi (Wallet Cash-Out)."));
                }
            }

            return Task.FromResult((false, (string?)null));
        }
    }
}
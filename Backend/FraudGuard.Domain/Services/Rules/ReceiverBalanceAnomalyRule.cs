using FraudGuard.Domain.Interfaces.Entities;
using FraudGuard.Domain.Common.Enums;
using FraudGuard.Domain.DomainObjects.TransactionProcessing;
using FraudGuard.Domain.Entities;
using FraudGuard.Domain.Interfaces.Repositories;
using FraudGuard.Domain.Interfaces.Rules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FraudGuard.Domain.Services.Rules
{
    public class ReceiverBalanceAnomalyRule : IFraudRule
    {
        private readonly IDebitCardRepository _debitCardRepository;
        private readonly ITransactionRepository _transactionRepository;

        public ReceiverBalanceAnomalyRule(IDebitCardRepository debitCardRepository, ITransactionRepository transactionRepository)
        {
            _debitCardRepository = debitCardRepository;
            _transactionRepository = transactionRepository;
        }

        public string RuleCode => "RECEIVER_BALANCE_ANOMALY";
        public string RuleName => "Katır Hesap Bakiye Sapması";

        public async Task<(bool IsSuspicious, string? Reason)> EvaluateAsync(ProcessTransactionInput input, List<ITransaction> history)
        {
            if (input.PaymentType == PaymentTypeEnum.EFT || input.PaymentType == PaymentTypeEnum.BankTransfer)
            {
                var receiverDebit = await _debitCardRepository.GetByIBANAsync(input.ReceiverIBAN);
                if (receiverDebit != null)
                {
                    var receiverRecentTx = await _transactionRepository.GetRecentTransactionsAsync(receiverDebit.CardId, isCreditCard: false, TimeSpan.FromDays(30));
                    bool isPassiveAccount = !receiverRecentTx.Any(t => t.Status == "Approved");
                    if (isPassiveAccount && input.Amount >= 5000)
                    {
                        return (true, $"Alıcı hesap ({input.ReceiverIBAN}) son 30 gündür pasif olmasına rağmen ani ve yüksek tutarlı (>= 5.000 TL) transfer gelmektedir.");
                    }
                }
            }

            return (false, null);
        }
    }
}
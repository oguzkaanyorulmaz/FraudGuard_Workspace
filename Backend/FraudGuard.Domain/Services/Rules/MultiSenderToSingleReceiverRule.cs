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
    public class MultiSenderToSingleReceiverRule : IFraudRule
    {
        private readonly ITransactionRepository _transactionRepository;

        public MultiSenderToSingleReceiverRule(ITransactionRepository transactionRepository)
        {
            _transactionRepository = transactionRepository;
        }

        public string RuleCode => "MULTI_SENDER_TO_SINGLE_RECEIVER";
        public string RuleName => "Tek Alıcıya Çoklu Kaynaktan Transfer";

        public async Task<(bool IsSuspicious, string? Reason)> EvaluateAsync(ProcessTransactionInput input, List<ETransaction> history)
        {
            if (input.PaymentType == PaymentTypeEnum.EFT || input.PaymentType == PaymentTypeEnum.BankTransfer)
            {
                var receiverHistory = await _transactionRepository.GetRecentTransactionsByReceiverIBANAsync(input.ReceiverIBAN, TimeSpan.FromMinutes(30));
                var distinctSendersCount = receiverHistory
                    .Where(t => t.Status == "Approved" && !string.IsNullOrEmpty(t.SenderIBAN) && t.SenderIBAN != input.SenderIBAN)
                    .Select(t => t.SenderIBAN)
                    .Distinct()
                    .Count();

                if (distinctSendersCount >= 3)
                {
                    return (true, $"Aynı alıcı hesaba ({input.ReceiverIBAN}) son 30 dakika içinde 4 veya daha fazla farklı kişiden para transferi yapılmaktadır.");
                }
            }

            return (false, null);
        }
    }
}

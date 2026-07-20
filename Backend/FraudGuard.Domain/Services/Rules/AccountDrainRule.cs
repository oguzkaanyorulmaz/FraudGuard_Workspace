using FraudGuard.Domain.Common.Enums;
using FraudGuard.Domain.DomainObjects.TransactionProcessing;
using FraudGuard.Domain.Entities;
using FraudGuard.Domain.Interfaces.Repositories;
using FraudGuard.Domain.Interfaces.Rules;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FraudGuard.Domain.Services.Rules
{
    public class AccountDrainRule : IFraudRule
    {
        private readonly IDebitCardRepository _debitCardRepository;

        public AccountDrainRule(IDebitCardRepository debitCardRepository)
        {
            _debitCardRepository = debitCardRepository;
        }

        public string RuleCode => "ACCOUNT_DRAIN";
        public string RuleName => "Hesap Boşaltma Denemesi (Account Drain - Banka Kartı)";

        public async Task<(bool IsSuspicious, string? Reason)> EvaluateAsync(ProcessTransactionInput input, List<ETransaction> history)
        {
            if (input.PaymentType == PaymentTypeEnum.DebitCard)
            {
                var dc = await _debitCardRepository.GetByCardNumberAsync(input.CardNumber);
                if (dc != null && dc.Balance > 0)
                {
                    if (input.Amount >= dc.Balance * 0.98m)
                    {
                        return (true, "Banka kartının bağlı olduğu mevduat hesabının %98 veya daha fazlası tek seferde çekilmek isteniyor.");
                    }
                }
            }

            return (false, null);
        }
    }
}

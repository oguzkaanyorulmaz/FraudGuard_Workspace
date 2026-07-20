using FraudGuard.Domain.Common.Enums;
using FraudGuard.Domain.DomainObjects.TransactionProcessing;
using FraudGuard.Domain.Entities;
using FraudGuard.Domain.Interfaces.Repositories;
using FraudGuard.Domain.Interfaces.Rules;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FraudGuard.Domain.Services.Rules
{
    public class MaxOutAttemptRule : IFraudRule
    {
        private readonly ICreditCardRepository _creditCardRepository;

        public MaxOutAttemptRule(ICreditCardRepository creditCardRepository)
        {
            _creditCardRepository = creditCardRepository;
        }

        public string RuleCode => "MAX_OUT";
        public string RuleName => "Limit Boşaltma Denemesi (Max-Out)";

        public async Task<(bool IsSuspicious, string? Reason)> EvaluateAsync(ProcessTransactionInput input, List<ETransaction> history)
        {
            if (input.PaymentType == PaymentTypeEnum.CreditCard)
            {
                var cc = await _creditCardRepository.GetByCardNumberAsync(input.CardNumber);
                if (cc != null && cc.AvailableLimit > 0)
                {
                    if (input.Amount >= cc.AvailableLimit * 0.95m)
                    {
                        return (true, "Kredi kartı kullanılabilir limitinin %95'i tek işlemle boşaltılmaya çalışılıyor.");
                    }
                }
            }

            return (false, null);
        }
    }
}
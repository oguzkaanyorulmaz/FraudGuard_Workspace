using FraudGuard.Domain.Entities;
using System.Collections.Generic;

namespace FraudGuard.Domain.CacheItems
{
    public class ActiveFraudRulesCache
    {
        public List<EFraudRule> ActiveRules { get; set; } = new List<EFraudRule>();
    }
}
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FraudGuard.Domain.Entities
{
    public class ETransactionCategory
    {
        [Key]
        public int CategoryId { get; set; }
        
        public string CategoryCode { get; set; }
        public string Description { get; set; }
        public int LimitEffect { get; set; } 
        
        public virtual ICollection<ETransaction> Transactions { get; set; }
    }
}
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations; // 1. Bu kütüphaneyi ekle

namespace FraudGuard.Domain.Entities
{
    public class ETransactionCategory
    {
        [Key] // 2. İşte EF Core'a bunun bir primary key olduğunu söylüyoruz
        public int CategoryId { get; set; }
        
        public string CategoryCode { get; set; }
        public string Description { get; set; }
        public int LimitEffect { get; set; } 
        
        public virtual ICollection<ETransaction> Transactions { get; set; }
    }
}
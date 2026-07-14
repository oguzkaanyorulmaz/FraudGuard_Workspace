using System;

namespace FraudGuard.Domain.Entities
{
    public class ECustomer
    {
        public int CustomerId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string IdentityNumber { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
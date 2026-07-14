using System;

namespace FraudGuard.Domain.Entities
{
    public class ECustomer
    {
        public int CustomerId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string IdentityNumber { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
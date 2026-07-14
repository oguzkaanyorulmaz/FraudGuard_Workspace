using System;

namespace FraudGuard.Application.DTOs
{
    public abstract class RequestDTO
    {
            public Guid RequestId { get; set; } = Guid.NewGuid();
        
        public DateTime RequestTime { get; set; } = DateTime.Now;
    }
}
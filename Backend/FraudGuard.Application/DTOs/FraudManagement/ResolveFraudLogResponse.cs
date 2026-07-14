using System;

namespace FraudGuard.Application.DTOs.FraudManagement
{
    public class ResolveFraudLogResponse
    {
        public bool IsResolvedSuccessfully { get; set; }
        public string ResultMessage { get; set; }
        public DateTime ResolvedAt { get; set; } = DateTime.Now;
    }
}
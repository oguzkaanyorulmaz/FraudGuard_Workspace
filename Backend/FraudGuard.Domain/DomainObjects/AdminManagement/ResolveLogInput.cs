using FraudGuard.Domain.Common.Enums;

namespace FraudGuard.Domain.DomainObjects.AdminManagement
{
    public class ResolveLogInput
    {
        public int LogId { get; set; }
        public AdminActionEnum Action { get; set; }
    }
}
namespace FraudGuard.Application.DTOs.FraudManagement
{
    public class ResolveFraudLogRequest : RequestDTO
    {
        public int LogId { get; set; }
        public string AdminAction { get; set; }
        public string AdminNote { get; set; } 
    public int? BlockReasonId { get; set; }
    public string? ResolvedByAdmin { get; set; }

    }
}